using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views.Dialogs;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using ClosedXML.Excel;

namespace CollegeJournalApp.Views.Pages
{
    public partial class SchedulePage : Page
    {
        private List<SchedRow> _all = new List<SchedRow>();

        private static readonly string[] DayNames =
            { "", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };

        private static readonly string[][] LessonTimes =
        {
            null,
            new[] { "08:30 – 09:15", "09:20 – 10:05" },
            new[] { "10:15 – 11:00", "11:05 – 11:50" },
            new[] { "12:30 – 13:15", "13:20 – 14:05" },
            new[] { "14:15 – 15:00", "15:05 – 15:50" },
            new[] { "16:00 – 16:45", "16:50 – 17:35" },
            new[] { "17:45 – 18:30", "18:35 – 19:20" }
        };

        public SchedulePage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => { SetupRoleUI(); LoadData(); };
        }

        // ── Настройка UI по роли ─────────────────────────────────────────────

        private void SetupRoleUI()
        {
            if (SessionHelper.IsAdmin)
            {
                PanelGroupFilter.Visibility   = Visibility.Visible;
                PanelTeacherFilter.Visibility = Visibility.Visible;
                BtnSearch.Visibility          = Visibility.Visible;
                BtnTemplate.Visibility        = Visibility.Visible;
                BtnImport.Visibility          = Visibility.Visible;
                LoadAdminFilters();
            }
            else if (SessionHelper.IsHeadman)
            {
                BtnImport.Visibility = Visibility.Visible;
            }
            // Teacher — только читает своё расписание, никаких фильтров и кнопок
        }

        private void LoadAdminFilters()
        {
            CmbGroup.Items.Clear();
            CmbGroup.Items.Add(new ComboBoxItem { Content = "— Все группы —", Tag = 0 });
            var groups = DatabaseHelper.ExecuteProcedure("sp_GetAllGroups", null);
            foreach (DataRow r in groups.Rows)
                CmbGroup.Items.Add(new ComboBoxItem
                {
                    Content = r["GroupName"]?.ToString(),
                    Tag     = Convert.ToInt32(r["GroupId"])
                });
            CmbGroup.SelectedIndex = 0;

            CmbTeacher.Items.Clear();
            CmbTeacher.Items.Add(new ComboBoxItem { Content = "— Все преподаватели —", Tag = 0 });
            var teachers = DatabaseHelper.ExecuteProcedure("sp_GetAllTeachers", null);
            foreach (DataRow r in teachers.Rows)
                CmbTeacher.Items.Add(new ComboBoxItem
                {
                    Content = r["FullName"]?.ToString(),
                    Tag     = Convert.ToInt32(r["TeacherId"])
                });
            CmbTeacher.SelectedIndex = 0;
        }

        // ── Загрузка данных ──────────────────────────────────────────────────

        private void LoadData()
        {
            try
            {
                if (SessionHelper.IsAdmin)
                {
                    ShowEmpty("Выберите группу или преподавателя для просмотра расписания");
                    TxtTotal.Text = "";
                    return;
                }

                DataTable dt;
                if (SessionHelper.IsTeacher)
                {
                    dt = DatabaseHelper.ExecuteProcedure("sp_GetTeacherSchedule",
                        new[] { new SqlParameter("@UserId", SessionHelper.UserId) });
                }
                else if (SessionHelper.IsCurator)
                {
                    dt = DatabaseHelper.ExecuteProcedure("sp_GetCuratorSchedule",
                        new[] { new SqlParameter("@UserId", SessionHelper.UserId) });
                }
                else
                {
                    dt = DatabaseHelper.ExecuteProcedure("sp_GetGroupSchedule",
                        new[]
                        {
                            new SqlParameter("@UserId",   SessionHelper.UserId),
                            new SqlParameter("@RoleName", SessionHelper.RoleName)
                        });
                }

                FillFromTable(dt);
                if (_all.Count == 0) ShowEmpty("Расписание не загружено");
                else                 BuildDayCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Поиск (Admin) ────────────────────────────────────────────────────

        private void CmbFilter_Changed(object sender, SelectionChangedEventArgs e)
            => BtnSearch_Click(null, null);

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            int groupId   = (int)((CmbGroup.SelectedItem   as ComboBoxItem)?.Tag ?? 0);
            int teacherId = (int)((CmbTeacher.SelectedItem as ComboBoxItem)?.Tag ?? 0);

            if (groupId == 0 && teacherId == 0)
            {
                ShowEmpty("Выберите группу или преподавателя для просмотра расписания");
                TxtTotal.Text = "";
                return;
            }

            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetScheduleAdmin", new[]
                {
                    new SqlParameter("@GroupId",   groupId   == 0 ? (object)DBNull.Value : groupId),
                    new SqlParameter("@TeacherId", teacherId == 0 ? (object)DBNull.Value : teacherId)
                });

                FillFromTable(dt);
                if (_all.Count == 0) ShowEmpty("По выбранным фильтрам занятий не найдено");
                else                 BuildDayCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Шаблон Excel ─────────────────────────────────────────────────────

        private void BtnTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                FileName = "Шаблон_расписания.xlsx",
                Filter   = "Excel файл|*.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Расписание");

                    // Заголовки — 7 колонок, Преподаватель необязателен
                    string[] headers = { "День", "№ пары", "Группа", "Предмет", "Аудитория", "Тип недели", "Преподаватель" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold            = true;
                        cell.Style.Font.FontColor       = XLColor.White;
                        cell.Style.Fill.BackgroundColor = i < 6
                            ? XLColor.FromHtml("#0078D4")   // обязательные — синие
                            : XLColor.FromHtml("#605E5C");  // необязательные — серые
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    // Подсказка в строке 2 колонки G
                    ws.Cell(2, 7).Value = "необязательно";
                    ws.Cell(2, 7).Style.Font.Italic    = true;
                    ws.Cell(2, 7).Style.Font.FontColor = XLColor.Gray;

                    // Пример данных
                    object[][] examples =
                    {
                        new object[] { "Понедельник", 1, "ЭБ-31", "Математика",   "301", "Обе",      "Иванов Иван Иванович"  },
                        new object[] { "Понедельник", 3, "ЭБ-31", "Информатика",  "205", "Чётная",   "Петров Пётр Петрович"  },
                        new object[] { "Вторник",     2, "ЭБ-31", "Физика",       "101", "Нечётная", "Сидорова Анна Петровна" },
                        new object[] { "Среда",       1, "ЭБ-31", "История",      "310", "Обе",      ""                       },
                    };
                    for (int ri = 0; ri < examples.Length; ri++)
                        for (int ci = 0; ci < examples[ri].Length; ci++)
                            ws.Cell(ri + 2, ci + 1).Value = XLCellValue.FromObject(examples[ri][ci]);

                    // Валидация: день недели
                    var dayVal = ws.Range("A2:A500").CreateDataValidation();
                    dayVal.InCellDropdown = true;
                    dayVal.AllowedValues  = XLAllowedValues.List;
                    dayVal.Value          = "\"Понедельник,Вторник,Среда,Четверг,Пятница,Суббота\"";
                    dayVal.ErrorTitle     = "Неверное значение";
                    dayVal.ErrorMessage   = "Выберите день из списка.";

                    // Валидация: номер пары
                    var lessonVal = ws.Range("B2:B500").CreateDataValidation();
                    lessonVal.AllowedValues  = XLAllowedValues.List;
                    lessonVal.Value          = "\"1,2,3,4,5,6\"";
                    lessonVal.InCellDropdown = true;

                    // Валидация: тип недели
                    var weekVal = ws.Range("F2:F500").CreateDataValidation();
                    weekVal.AllowedValues    = XLAllowedValues.List;
                    weekVal.Value            = "\"Обе,Чётная,Нечётная\"";
                    weekVal.InCellDropdown   = true;

                    // Ширина колонок
                    int[] widths = { 16, 10, 14, 30, 12, 14, 26 };
                    for (int i = 0; i < widths.Length; i++)
                        ws.Column(i + 1).Width = widths[i];

                    wb.SaveAs(dlg.FileName);
                }

                MessageBox.Show("Шаблон сохранён!\n\nЗаполните его и загрузите через «Загрузить из Excel».",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании шаблона:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Импорт из Excel ──────────────────────────────────────────────────

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel файлы|*.xlsx;*.xls|Все файлы|*.*",
                Title  = "Выберите файл с расписанием"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                int ok = 0, skip = 0;
                var errors = new System.Text.StringBuilder();

                using (var wb = new XLWorkbook(dlg.FileName))
                {
                    var ws  = wb.Worksheet(1);
                    int row = 2;

                    while (!ws.Cell(row, 1).IsEmpty())
                    {
                        string dayName     = ws.Cell(row, 1).GetValue<string>().Trim();
                        string lessonStr   = ws.Cell(row, 2).GetValue<string>().Trim();
                        string groupName   = ws.Cell(row, 3).GetValue<string>().Trim();
                        string subject     = ws.Cell(row, 4).GetValue<string>().Trim();
                        string classroom   = ws.Cell(row, 5).GetValue<string>().Trim();
                        string weekType    = ws.Cell(row, 6).GetValue<string>().Trim();
                        string teacherName = ws.Cell(row, 7).GetValue<string>().Trim(); // необязательно

                        if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(subject))
                        { row++; skip++; continue; }

                        int dayNum = Array.IndexOf(DayNames, dayName);
                        if (dayNum <= 0) { row++; skip++; continue; }

                        if (!byte.TryParse(lessonStr, out byte lessonNum) || lessonNum < 1 || lessonNum > 6)
                        { row++; skip++; continue; }

                        if (string.IsNullOrEmpty(weekType)) weekType = "Обе";

                        try
                        {
                            DatabaseHelper.ExecuteNonQuery("sp_ImportScheduleItem", new[]
                            {
                                new SqlParameter("@GroupName",    groupName),
                                new SqlParameter("@DayOfWeek",    (byte)dayNum),
                                new SqlParameter("@LessonNumber", lessonNum),
                                new SqlParameter("@SubjectName",  subject),
                                new SqlParameter("@Classroom",    string.IsNullOrEmpty(classroom)   ? (object)DBNull.Value : classroom),
                                new SqlParameter("@WeekType",     weekType),
                                new SqlParameter("@TeacherName",  string.IsNullOrEmpty(teacherName) ? (object)DBNull.Value : teacherName)
                            });
                            ok++;
                        }
                        catch (Exception rowEx)
                        {
                            errors.AppendLine($"Строка {row}: {rowEx.Message}");
                            skip++;
                        }

                        row++;
                    }
                }

                string msg = $"Импорт завершён.\nЗагружено строк: {ok}";
                if (skip > 0) msg += $"\nПропущено: {skip}";
                if (errors.Length > 0) msg += $"\n\nОшибки:\n{errors}";

                MessageBox.Show(msg, "Импорт", MessageBoxButton.OK,
                    errors.Length > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

                // Обновить вид
                if (SessionHelper.IsAdmin && ok > 0) BtnSearch_Click(null, null);
                else if (ok > 0)                     LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении файла:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Вспомогательные методы ───────────────────────────────────────────

        private void FillFromTable(DataTable dt)
        {
            bool hasGroup      = dt.Columns.Contains("GroupName");
            bool hasScheduleId = dt.Columns.Contains("ScheduleId");
            bool hasGroupId    = dt.Columns.Contains("GroupId");
            bool hasSubjectId  = dt.Columns.Contains("SubjectId");
            bool hasTeacher    = dt.Columns.Contains("TeacherName");

            _all.Clear();
            foreach (DataRow r in dt.Rows)
            {
                int dow = r["DayOfWeek"] != DBNull.Value ? Convert.ToInt32(r["DayOfWeek"]) : 0;
                _all.Add(new SchedRow
                {
                    ScheduleId = hasScheduleId && r["ScheduleId"] != DBNull.Value ? Convert.ToInt32(r["ScheduleId"]) : 0,
                    DayNum     = dow,
                    LessonNum  = r["LessonNumber"]?.ToString() ?? "",
                    Subject    = r["SubjectName"]?.ToString()  ?? "—",
                    Classroom  = r["Classroom"]?.ToString()    ?? "—",
                    Teacher    = hasTeacher && r["TeacherName"] != DBNull.Value ? r["TeacherName"]?.ToString() ?? "—" : "—",
                    GroupName  = hasGroup     && r["GroupName"]  != DBNull.Value ? r["GroupName"]?.ToString()  ?? "" : "",
                    GroupId    = hasGroupId   && r["GroupId"]    != DBNull.Value ? Convert.ToInt32(r["GroupId"])    : 0,
                    SubjectId  = hasSubjectId && r["SubjectId"]  != DBNull.Value ? Convert.ToInt32(r["SubjectId"])  : 0
                });
            }
            TxtTotal.Text = $"— {_all.Count} занятий";
        }

        private void ShowEmpty(string message)
        {
            TxtEmpty.Text          = message;
            EmptyState.Visibility  = Visibility.Visible;
            CardsScroll.Visibility = Visibility.Collapsed;
        }

        private int GetAdminGroupId() =>
            (int)((CmbGroup?.SelectedItem as ComboBoxItem)?.Tag ?? 0);

        // ── Текущая пара ─────────────────────────────────────────────────────

        private int GetCurrentLessonNum()
        {
            var now = DateTime.Now.TimeOfDay;
            for (int i = 1; i <= 6; i++)
            {
                var times    = LessonTimes[i];
                var startStr = times[0].Split(new[] { " – " }, StringSplitOptions.None)[0].Trim();
                var endStr   = times[1].Split(new[] { " – " }, StringSplitOptions.None)[1].Trim();
                if (TimeSpan.TryParse(startStr, out var start) &&
                    TimeSpan.TryParse(endStr,   out var end)   &&
                    now >= start && now <= end)
                    return i;
            }
            return 0;
        }

        // ── Построение карточек дней ─────────────────────────────────────────

        private void BuildDayCards()
        {
            EmptyState.Visibility  = Visibility.Collapsed;
            CardsScroll.Visibility = Visibility.Visible;

            var grid = DayCardsGrid;
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            for (int c = 0; c < 3; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            bool showGroup  = SessionHelper.IsCurator || SessionHelper.IsAdmin || SessionHelper.IsTeacher;
            bool isAdmin    = SessionHelper.IsAdmin;

            // Цвета
            var borderBrush  = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            var headerBg     = new SolidColorBrush(Color.FromRgb(0xF3, 0xF2, 0xF1));
            var accentBlue   = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
            var lightBlue    = new SolidColorBrush(Color.FromRgb(0xF0, 0xF7, 0xFF));
            var todayHeader  = new SolidColorBrush(Color.FromRgb(0xDE, 0xEC, 0xF8));
            var todayText    = new SolidColorBrush(Color.FromRgb(0x00, 0x56, 0x9B));
            var textPrimary  = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x1F));
            var textMuted    = new SolidColorBrush(Color.FromRgb(0x96, 0x94, 0x92));
            var emptyBg      = new SolidColorBrush(Color.FromRgb(0xFD, 0xFD, 0xFD));
            var badgeGray    = new SolidColorBrush(Color.FromRgb(0xC8, 0xC6, 0xC4));
            var redBrush     = new SolidColorBrush(Color.FromRgb(0xD1, 0x34, 0x38));
            // Текущая пара — зелёный акцент
            var nowGreen     = new SolidColorBrush(Color.FromRgb(0x10, 0x79, 0x27));
            var nowGreenBg   = new SolidColorBrush(Color.FromRgb(0xF0, 0xFB, 0xF2));
            var nowGreenBadge= new SolidColorBrush(Color.FromRgb(0xDC, 0xF5, 0xE0));

            int rawDow        = (int)DateTime.Today.DayOfWeek;
            int todayDow      = rawDow == 0 ? 7 : rawDow;
            int currentLesson = GetCurrentLessonNum(); // 0 если не идёт пара

            for (int dayNum = 1; dayNum <= 6; dayNum++)
            {
                int  col     = (dayNum - 1) % 3;
                int  row     = (dayNum - 1) / 3;
                bool isToday = dayNum == todayDow;
                int  capturedDay = dayNum;

                var card = new Border
                {
                    Margin          = new Thickness(8),
                    Background      = Brushes.White,
                    BorderBrush     = isToday ? accentBlue : borderBrush,
                    BorderThickness = new Thickness(isToday ? 2 : 1),
                    CornerRadius    = new CornerRadius(8),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black, Opacity = 0.08,
                        BlurRadius = 10, ShadowDepth = 2, Direction = 270
                    }
                };

                var cardGrid = new Grid();
                cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int i = 0; i < 6; i++)
                    cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Заголовок дня
                var header = new Border
                {
                    Background   = isToday ? accentBlue : headerBg,
                    CornerRadius = new CornerRadius(7, 7, 0, 0),
                    Padding      = new Thickness(12, 10, 12, 10)
                };
                header.Child = new TextBlock
                {
                    Text       = DayNames[dayNum],
                    FontSize   = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = isToday ? Brushes.White : textPrimary
                };
                Grid.SetRow(header, 0);
                Grid.SetColumnSpan(header, 3);
                cardGrid.Children.Add(header);

                // Строки пар
                for (int i = 0; i < 6; i++)
                {
                    int  lessonNum    = i + 1;
                    bool lastRow      = i == 5;
                    int  capturedLesson = lessonNum;
                    var  lesson       = _all.FirstOrDefault(x =>
                        x.DayNum == dayNum && x.LessonNum == lessonNum.ToString());
                    bool has       = lesson != null;
                    var  times     = LessonTimes[lessonNum];
                    bool isNow     = isToday && has && lessonNum == currentLesson;
                    var  rowBg     = isNow ? nowGreenBg : (has ? Brushes.White : emptyBg);
                    var  accentBar = isNow ? nowGreen   : accentBlue;

                    // Ячейка №
                    var numCell = new Border
                    {
                        Background        = headerBg,
                        BorderBrush       = borderBrush,
                        BorderThickness   = new Thickness(0, 0, 1, lastRow ? 0 : 1),
                        VerticalAlignment = VerticalAlignment.Stretch
                    };
                    var badge = new Border
                    {
                        Background          = isNow ? nowGreen : (has ? accentBlue : badgeGray),
                        CornerRadius        = new CornerRadius(10),
                        Width               = 22,
                        Padding             = new Thickness(0, 2, 0, 2),
                        Margin              = new Thickness(0, 10, 0, 10),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment   = VerticalAlignment.Center
                    };
                    badge.Child = new TextBlock
                    {
                        Text                = lessonNum.ToString(),
                        FontSize            = 10,
                        FontWeight          = FontWeights.Bold,
                        Foreground          = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    numCell.Child = badge;
                    Grid.SetRow(numCell, i + 1);
                    Grid.SetColumn(numCell, 0);
                    cardGrid.Children.Add(numCell);

                    // Ячейка времени
                    var timeCell = new Border
                    {
                        Background      = rowBg,
                        BorderBrush     = borderBrush,
                        BorderThickness = new Thickness(0, 0, 1, lastRow ? 0 : 1),
                        Padding         = new Thickness(6, 8, 6, 8)
                    };
                    var timeStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    timeStack.Children.Add(new TextBlock { Text = times[0], FontSize = 10, Foreground = isNow ? nowGreen : (has ? textPrimary : textMuted), FontWeight = isNow ? FontWeights.SemiBold : FontWeights.Normal });
                    timeStack.Children.Add(new TextBlock { Text = times[1], FontSize = 10, Foreground = isNow ? nowGreen : (has ? textPrimary : textMuted), Margin = new Thickness(0, 2, 0, 0), FontWeight = isNow ? FontWeights.SemiBold : FontWeights.Normal });
                    timeCell.Child = timeStack;
                    Grid.SetRow(timeCell, i + 1);
                    Grid.SetColumn(timeCell, 1);
                    cardGrid.Children.Add(timeCell);

                    // Ячейка предмета
                    var subjectCell = new Border
                    {
                        Background      = rowBg,
                        BorderBrush     = borderBrush,
                        BorderThickness = new Thickness(0, 0, 0, lastRow ? 0 : 1),
                        MinHeight       = 62
                    };

                    if (has)
                    {
                        // Контент + кнопки редактирования для Admin
                        var outerStack = new StackPanel();

                        var inner = new Border
                        {
                            BorderBrush     = accentBar,
                            BorderThickness = new Thickness(3, 0, 0, 0),
                            Margin          = new Thickness(8, 8, 8, isAdmin ? 4 : 8),
                            Padding         = new Thickness(8, 4, 6, 4)
                        };
                        var sp = new StackPanel();

                        // Бейдж "▶ Идёт сейчас"
                        if (isNow)
                        {
                            var nowBadge = new Border
                            {
                                Background          = nowGreenBadge,
                                CornerRadius        = new CornerRadius(4),
                                Padding             = new Thickness(5, 2, 5, 2),
                                Margin              = new Thickness(0, 0, 0, 4),
                                HorizontalAlignment = HorizontalAlignment.Left
                            };
                            nowBadge.Child = new TextBlock
                            {
                                Text       = "▶  Идёт сейчас",
                                FontSize   = 10,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = nowGreen
                            };
                            sp.Children.Add(nowBadge);
                        }

                        sp.Children.Add(new TextBlock
                        {
                            Text         = lesson.Subject,
                            FontSize     = 12,
                            FontWeight   = FontWeights.SemiBold,
                            Foreground   = isNow ? nowGreen : textPrimary,
                            TextWrapping = TextWrapping.Wrap
                        });
                        sp.Children.Add(new TextBlock
                        {
                            Text       = "ауд. " + lesson.Classroom,
                            FontSize   = 11,
                            Foreground = textMuted,
                            Margin     = new Thickness(0, 3, 0, 0)
                        });
                        if (showGroup && !string.IsNullOrEmpty(lesson.GroupName))
                            sp.Children.Add(new TextBlock
                            {
                                Text       = "гр. " + lesson.GroupName,
                                FontSize   = 10,
                                Foreground = isNow ? nowGreen : accentBlue,
                                Margin     = new Thickness(0, 2, 0, 0)
                            });

                        inner.Child = sp;
                        outerStack.Children.Add(inner);

                        // Кнопки Admin: Изменить / Удалить
                        if (isAdmin)
                        {
                            var btnBar = new StackPanel
                            {
                                Orientation         = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Margin              = new Thickness(0, 0, 8, 6)
                            };

                            var capturedLesson2 = lesson; // захват

                            var btnEdit = MakeActionButton("✎ Изменить", "#0078D4");
                            btnEdit.Click += (s, ev) => OpenEditDialog(capturedLesson2);
                            btnBar.Children.Add(btnEdit);

                            var btnDel = MakeActionButton("✕ Удалить", "#D13438");
                            btnDel.Click += (s, ev) => DeleteLesson(capturedLesson2);
                            btnBar.Children.Add(btnDel);

                            outerStack.Children.Add(btnBar);
                        }

                        subjectCell.Child = outerStack;
                    }
                    else if (isAdmin)
                    {
                        // Пустая ячейка со знаком «+» для Admin
                        var addBtn = new Button
                        {
                            Content             = "+ Добавить",
                            FontSize            = 11,
                            Foreground          = textMuted,
                            Background          = Brushes.Transparent,
                            BorderThickness     = new Thickness(0),
                            Cursor              = System.Windows.Input.Cursors.Hand,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment   = VerticalAlignment.Center,
                            Margin              = new Thickness(0, 10, 0, 10)
                        };
                        int prefillGroup = GetAdminGroupId();
                        addBtn.Click += (s, ev) => OpenAddDialog(capturedDay, capturedLesson, prefillGroup);
                        subjectCell.Child = addBtn;
                    }

                    Grid.SetRow(subjectCell, i + 1);
                    Grid.SetColumn(subjectCell, 2);
                    cardGrid.Children.Add(subjectCell);
                }

                card.Child = cardGrid;
                Grid.SetRow(card, row);
                Grid.SetColumn(card, col);
                grid.Children.Add(card);
            }
        }

        // ── Кнопки Admin: Добавить / Изменить / Удалить ──────────────────────

        private void OpenAddDialog(int dayNum, int lessonNum, int prefillGroupId)
        {
            var dlg = new ScheduleEditDialog(
                prefillGroupId > 0 ? prefillGroupId : (int?)null, dayNum, lessonNum)
            { Owner = Window.GetWindow(this) };
            dlg.ShowDialog();
            if (dlg.Saved) BtnSearch_Click(null, null);
        }

        private void OpenEditDialog(SchedRow row)
        {
            var dlg = new ScheduleEditDialog(
                row.ScheduleId, row.GroupId, row.DayNum,
                int.TryParse(row.LessonNum, out int ln) ? ln : 1,
                row.SubjectId, row.Classroom, "Обе")
            { Owner = Window.GetWindow(this) };
            dlg.ShowDialog();
            if (dlg.Saved) BtnSearch_Click(null, null);
        }

        private void DeleteLesson(SchedRow row)
        {
            var res = MessageBox.Show(
                $"Удалить пару «{row.Subject}» ({DayNames[row.DayNum]}, пара №{row.LessonNum})?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_DeleteScheduleItem", new[]
                {
                    new SqlParameter("@ScheduleId",  row.ScheduleId),
                    new SqlParameter("@DeletedById", SessionHelper.UserId)
                });
                BtnSearch_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Вспомогательный метод создания кнопки ────────────────────────────

        private static Button MakeActionButton(string text, string hexColor)
        {
            return new Button
            {
                Content         = text,
                FontSize        = 10,
                Height          = 22,
                Padding         = new Thickness(6, 0, 6, 0),
                Margin          = new Thickness(4, 0, 0, 0),
                Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor)),
                Foreground      = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor          = System.Windows.Input.Cursors.Hand
            };
        }
    }

    public class SchedRow
    {
        public int    ScheduleId { get; set; }
        public int    DayNum     { get; set; }
        public string LessonNum  { get; set; }
        public string Subject    { get; set; }
        public string Classroom  { get; set; }
        public string Teacher    { get; set; }
        public string GroupName  { get; set; }
        public int    GroupId    { get; set; }
        public int    SubjectId  { get; set; }
    }
}
