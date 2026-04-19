using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ClosedXML.Excel;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CollegeJournalApp.Views.Pages
{
    public partial class GradesPage : Page
    {
        // ── Состояние ──────────────────────────────────────────────────────
        private int  _year, _month, _daysInMonth;
        private bool _isJournalView = true;

        private readonly List<IdNameG> _groups   = new List<IdNameG>();
        private readonly List<IdNameG> _subjects = new List<IdNameG>();

        private int _currentGroupId   = 0;
        private int _currentSubjectId = 0;

        // Предметы, по которым текущий пользователь может ставить оценки
        private readonly HashSet<int> _editableSubjectIds = new HashSet<int>();

        // Словарь меток колонок (DataGridColumn.Tag не существует — используем свой)
        private readonly Dictionary<DataGridColumn, string> _colTags
            = new Dictionary<DataGridColumn, string>();

        // Попап ввода оценки
        private Popup   _gradePopup;
        private int     _popStudentId, _popSubjectId;
        private DateTime? _popDate;          // null → итоговая за месяц

        private bool CanEdit =>
            SessionHelper.RoleName == "Admin" ||
            SessionHelper.RoleName == "Curator";

        // ── Конструктор ────────────────────────────────────────────────────
        public GradesPage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => Init();
        }

        private void Init()
        {
            _year  = DateTime.Today.Year;
            _month = DateTime.Today.Month;
            UpdateMonthLabel();
            BuildGradePopup();
            LoadGroups();

            // Закрываем попап при клике за его пределами
            Loaded   += (s, e) =>
            {
                var win = Window.GetWindow(this);
                if (win != null) win.PreviewMouseDown += Win_PreviewMouseDown;
            };
            Unloaded += (s, e) =>
            {
                var win = Window.GetWindow(this);
                if (win != null) win.PreviewMouseDown -= Win_PreviewMouseDown;
            };
        }

        private void Win_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(_gradePopup?.IsOpen == true)) return;
            var content = _gradePopup.Child as UIElement;
            // Если клик НЕ на попапе — закрываем
            if (content == null || !content.IsMouseOver)
                _gradePopup.IsOpen = false;
        }

        // ── Группы ─────────────────────────────────────────────────────────
        private void LoadGroups()
        {
            _groups.Clear();
            CmbGroup.SelectionChanged -= CmbGroup_Changed;
            CmbGroup.Items.Clear();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetGroupsForUser", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });
                foreach (DataRow r in dt.Rows)
                    _groups.Add(new IdNameG(Convert.ToInt32(r["GroupId"]), r["GroupName"]?.ToString() ?? ""));
            }
            catch { }

            foreach (var g in _groups) CmbGroup.Items.Add(g.Name);
            CmbGroup.SelectionChanged += CmbGroup_Changed;
            if (_groups.Count > 0) CmbGroup.SelectedIndex = 0;
        }

        private void CmbGroup_Changed(object sender, SelectionChangedEventArgs e)
        {
            int idx = CmbGroup.SelectedIndex;
            if (idx < 0 || idx >= _groups.Count) return;
            _currentGroupId = _groups[idx].Id;
            LoadSubjects();
        }

        // ── Предметы ───────────────────────────────────────────────────────
        private void LoadSubjects()
        {
            _subjects.Clear();
            _editableSubjectIds.Clear();
            CmbSubject.SelectionChanged -= CmbSubject_Changed;
            CmbSubject.Items.Clear();
            if (_currentGroupId == 0) { CmbSubject.SelectionChanged += CmbSubject_Changed; return; }

            try
            {
                // Для Admin/Headman/Student — все предметы группы.
                // Для Curator/Teacher — только свои предметы (фильтрация в SP по Teachers.UserId).
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetSubjectsForGrades", new[]
                {
                    new SqlParameter("@GroupId",  _currentGroupId),
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });
                foreach (DataRow r in dt.Rows)
                {
                    int id = Convert.ToInt32(r["SubjectId"]);
                    _subjects.Add(new IdNameG(id, r["SubjectName"]?.ToString() ?? ""));
                    _editableSubjectIds.Add(id);  // все загруженные предметы = редактируемые
                }
            }
            catch { }

            foreach (var s in _subjects) CmbSubject.Items.Add(s.Name);
            CmbSubject.SelectionChanged += CmbSubject_Changed;

            if (_subjects.Count > 0) CmbSubject.SelectedIndex = 0;
            else Refresh();
        }

        private void CmbSubject_Changed(object sender, SelectionChangedEventArgs e)
        {
            int idx = CmbSubject.SelectedIndex;
            if (idx < 0 || idx >= _subjects.Count) return;
            _currentSubjectId = _subjects[idx].Id;
            Refresh();
        }

        // ── Навигация по месяцам ───────────────────────────────────────────
        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            var d = new DateTime(_year, _month, 1).AddMonths(-1);
            _year = d.Year; _month = d.Month;
            UpdateMonthLabel(); Refresh();
        }
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            var d = new DateTime(_year, _month, 1).AddMonths(1);
            _year = d.Year; _month = d.Month;
            UpdateMonthLabel(); Refresh();
        }
        private void UpdateMonthLabel()
            => TxtMonth.Text = new DateTime(_year, _month, 1)
                .ToString("MMMM yyyy", new CultureInfo("ru-RU")).ToUpper();

        // ── Переключение режимов ───────────────────────────────────────────
        private void BtnJournal_Click(object sender, RoutedEventArgs e)
        {
            if (_isJournalView) return;
            _isJournalView = true;
            SetTabStyle(true);
            LblSubject.Visibility  = Visibility.Visible;
            CmbSubject.Visibility  = Visibility.Visible;
            Refresh();
        }
        private void BtnStatement_Click(object sender, RoutedEventArgs e)
        {
            if (!_isJournalView) return;
            _isJournalView = false;
            SetTabStyle(false);
            LblSubject.Visibility  = Visibility.Collapsed;
            CmbSubject.Visibility  = Visibility.Collapsed;
            Refresh();
        }
        private void SetTabStyle(bool journalActive)
        {
            var blue  = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            var grey  = new SolidColorBrush(Color.FromRgb(208, 208, 208));
            BtnJournal.Background   = journalActive  ? blue  : Brushes.White;
            BtnJournal.Foreground   = journalActive  ? Brushes.White : new SolidColorBrush(Color.FromRgb(50,49,48));
            BtnJournal.BorderBrush  = journalActive  ? blue  : grey;
            BtnStatement.Background = !journalActive ? blue  : Brushes.White;
            BtnStatement.Foreground = !journalActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(50,49,48));
            BtnStatement.BorderBrush= !journalActive ? blue  : grey;
        }

        private void Refresh()
        {
            if (_currentGroupId == 0) return;
            if (_isJournalView) LoadJournal();
            else                LoadStatement();
        }

        // ── ЖУРНАЛ ─────────────────────────────────────────────────────────
        private void LoadJournal()
        {
            if (_currentSubjectId == 0) { ClearGrid(); return; }

            _daysInMonth = DateTime.DaysInMonth(_year, _month);

            try
            {
                var dtGrades = DatabaseHelper.ExecuteProcedure("sp_GetJournalData", new[]
                {
                    new SqlParameter("@GroupId",   _currentGroupId),
                    new SqlParameter("@SubjectId", _currentSubjectId),
                    new SqlParameter("@Year",      _year),
                    new SqlParameter("@Month",     _month)
                });
                var dtMonthly = DatabaseHelper.ExecuteProcedure("sp_GetJournalMonthly", new[]
                {
                    new SqlParameter("@GroupId",   _currentGroupId),
                    new SqlParameter("@SubjectId", _currentSubjectId),
                    new SqlParameter("@Year",      _year),
                    new SqlParameter("@Month",     _month)
                });

                // Словарь итоговых оценок
                var monthly = new Dictionary<int, (decimal? avg, int? final, bool over)>();
                foreach (DataRow r in dtMonthly.Rows)
                {
                    int sid = Convert.ToInt32(r["StudentId"]);
                    monthly[sid] = (
                        r["CalculatedAvg"] != DBNull.Value ? (decimal?)Convert.ToDecimal(r["CalculatedAvg"]) : null,
                        r["FinalGrade"]    != DBNull.Value ? (int?)Convert.ToInt32(r["FinalGrade"])           : null,
                        r["IsOverridden"]  != DBNull.Value && Convert.ToBoolean(r["IsOverridden"])
                    );
                }

                // Строим DataTable
                var table = new DataTable();
                table.Columns.Add("StudentId",   typeof(int));
                table.Columns.Add("StudentName", typeof(string));
                for (int d = 1; d <= _daysInMonth; d++)
                    table.Columns.Add($"d{d:00}", typeof(string));
                table.Columns.Add("Avg",   typeof(string));
                table.Columns.Add("Final", typeof(string));

                // Пивот
                var rows = new Dictionary<int, DataRow>();
                foreach (DataRow r in dtGrades.Rows)
                {
                    int sid = Convert.ToInt32(r["StudentId"]);
                    if (!rows.ContainsKey(sid))
                    {
                        var dr = table.NewRow();
                        dr["StudentId"]   = sid;
                        dr["StudentName"] = r["StudentName"]?.ToString() ?? "";
                        rows[sid] = dr;
                        table.Rows.Add(dr);
                    }
                    if (r["GradeDate"] != DBNull.Value && r["GradeValue"] != DBNull.Value)
                    {
                        int day = Convert.ToDateTime(r["GradeDate"]).Day;
                        rows[sid][$"d{day:00}"] = r["GradeValue"].ToString();
                    }
                }

                // Среднее и итог
                foreach (DataRow dr in table.Rows)
                {
                    int sid = Convert.ToInt32(dr["StudentId"]);

                    if (monthly.TryGetValue(sid, out var mg) && mg.avg.HasValue)
                    {
                        dr["Avg"] = mg.avg.Value.ToString("F1", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        var vals = Enumerable.Range(1, _daysInMonth)
                            .Select(d => dr[$"d{d:00}"]?.ToString())
                            .Where(v => !string.IsNullOrEmpty(v) && int.TryParse(v, out _))
                            .Select(v => (double)int.Parse(v)).ToList();
                        dr["Avg"] = vals.Count > 0 ? vals.Average().ToString("F1", CultureInfo.InvariantCulture) : "";
                    }

                    if (monthly.TryGetValue(sid, out mg) && mg.final.HasValue)
                        dr["Final"] = mg.final.Value + (mg.over ? "*" : "");
                    else if (!string.IsNullOrEmpty(dr["Avg"]?.ToString()))
                        if (double.TryParse(dr["Avg"].ToString(), NumberStyles.Any,
                                CultureInfo.InvariantCulture, out double avg))
                            dr["Final"] = ((int)Math.Round(avg)).ToString();
                }

                BuildJournalColumns(_daysInMonth);
                MainGrid.ItemsSource    = table.DefaultView;
                MainGrid.FrozenColumnCount = 1;
                TxtSubtitle.Text = $"— {table.Rows.Count} студентов";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки журнала:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildJournalColumns(int days)
        {
            MainGrid.Columns.Clear();
            _colTags.Clear();

            // Студент
            var nameCol = new DataGridTextColumn
            {
                Header  = "Студент",
                Binding = new Binding("[StudentName]"),
                Width   = new DataGridLength(185)
            };
            nameCol.ElementStyle = NameCellStyle();
            _colTags[nameCol] = "name";
            MainGrid.Columns.Add(nameCol);

            // Дни
            for (int d = 1; d <= days; d++)
                MainGrid.Columns.Add(MakeGradeCol(d.ToString(), $"[d{d:00}]", $"day:{d}", 30));

            // Среднее
            var avgCol = new DataGridTextColumn
            {
                Header  = "Ср.",
                Binding = new Binding("[Avg]"),
                Width   = 46
            };
            avgCol.ElementStyle = CenteredBoldStyle(Color.FromRgb(50, 49, 48));
            _colTags[avgCol] = "avg";
            MainGrid.Columns.Add(avgCol);

            // Итог
            MainGrid.Columns.Add(MakeGradeCol("Итог", "[Final]", "final", 50));
        }

        // ── ВЕДОМОСТЬ ──────────────────────────────────────────────────────
        private void LoadStatement()
        {
            if (_currentGroupId == 0) { ClearGrid(); return; }

            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetMonthlyStatement", new[]
                {
                    new SqlParameter("@GroupId", _currentGroupId),
                    new SqlParameter("@Year",    _year),
                    new SqlParameter("@Month",   _month)
                });

                // Собираем уникальные предметы
                var subjIds   = new List<int>();
                var subjNames = new List<string>();
                foreach (DataRow r in dt.Rows)
                {
                    int sid = Convert.ToInt32(r["SubjectId"]);
                    if (!subjIds.Contains(sid))
                    {
                        subjIds.Add(sid);
                        subjNames.Add(r["SubjectName"]?.ToString() ?? "—");
                    }
                }

                // DataTable
                var table = new DataTable();
                table.Columns.Add("StudentId",   typeof(int));
                table.Columns.Add("StudentName", typeof(string));
                foreach (int sid in subjIds)
                    table.Columns.Add($"s{sid}", typeof(string));

                var rowDict = new Dictionary<int, DataRow>();
                foreach (DataRow r in dt.Rows)
                {
                    int stId = Convert.ToInt32(r["StudentId"]);
                    int sbId = Convert.ToInt32(r["SubjectId"]);
                    if (!rowDict.ContainsKey(stId))
                    {
                        var dr = table.NewRow();
                        dr["StudentId"]   = stId;
                        dr["StudentName"] = r["StudentName"]?.ToString() ?? "—";
                        rowDict[stId]     = dr;
                        table.Rows.Add(dr);
                    }
                    if (r["FinalGrade"] != DBNull.Value)
                    {
                        bool over = r["IsOverridden"] != DBNull.Value && Convert.ToBoolean(r["IsOverridden"]);
                        rowDict[stId][$"s{sbId}"] = r["FinalGrade"] + (over ? "*" : "");
                    }
                }

                // Колонки
                MainGrid.Columns.Clear();
                _colTags.Clear();
                var nameCol = new DataGridTextColumn
                {
                    Header  = "Студент",
                    Binding = new Binding("[StudentName]"),
                    Width   = new DataGridLength(185)
                };
                nameCol.ElementStyle = NameCellStyle();
                _colTags[nameCol] = "name";
                MainGrid.Columns.Add(nameCol);

                for (int i = 0; i < subjIds.Count; i++)
                {
                    string shortName = subjNames[i].Length > 18
                        ? subjNames[i].Substring(0, 16) + "…"
                        : subjNames[i];
                    var hdr = new TextBlock
                    {
                        Text         = shortName,
                        ToolTip      = subjNames[i],
                        TextWrapping = TextWrapping.NoWrap
                    };
                    var col = MakeGradeCol(shortName, $"[s{subjIds[i]}]", $"subj:{subjIds[i]}", 60);
                    col.Header = hdr;
                    MainGrid.Columns.Add(col);
                }

                MainGrid.ItemsSource    = table.DefaultView;
                MainGrid.FrozenColumnCount = 1;
                TxtSubtitle.Text = $"— {table.Rows.Count} студентов, {subjIds.Count} предметов";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки ведомости:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Построение ячеек с оценками ────────────────────────────────────
        private DataGridTemplateColumn MakeGradeCol(string header, string bindPath, string tag, double width)
        {
            var col = new DataGridTemplateColumn
            {
                Header = header,
                Width  = width
            };
            _colTags[col] = tag;

            // Border с цветом фона
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.MarginProperty,              new Thickness(3, 4, 3, 4));
            border.SetValue(Border.CornerRadiusProperty,        new CornerRadius(3));
            border.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            border.SetBinding(Border.BackgroundProperty,
                new Binding(bindPath) { Converter = GradeBackConv.Instance });

            // TextBlock с оценкой
            var tb = new FrameworkElementFactory(typeof(TextBlock));
            tb.SetBinding(TextBlock.TextProperty,
                new Binding(bindPath) { Converter = GradeTextConv.Instance });
            tb.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            tb.SetValue(TextBlock.VerticalAlignmentProperty,   VerticalAlignment.Center);
            tb.SetValue(TextBlock.FontSizeProperty,            12.0);
            tb.SetValue(TextBlock.FontWeightProperty,          FontWeights.SemiBold);
            tb.SetValue(TextBlock.PaddingProperty,             new Thickness(2, 1, 2, 1));
            tb.SetBinding(TextBlock.ForegroundProperty,
                new Binding(bindPath) { Converter = GradeForeConv.Instance });

            border.AppendChild(tb);
            col.CellTemplate = new DataTemplate { VisualTree = border };
            return col;
        }

        private static Style NameCellStyle()
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(TextBlock.MarginProperty,   new Thickness(10, 0, 8, 0)));
            s.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            return s;
        }

        private static Style CenteredBoldStyle(Color color)
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.FontWeightProperty,          FontWeights.SemiBold));
            s.Setters.Add(new Setter(TextBlock.ForegroundProperty,
                new SolidColorBrush(color)));
            return s;
        }

        private void ClearGrid()
        {
            _colTags.Clear();
            MainGrid.Columns.Clear();
            MainGrid.ItemsSource = null;
            TxtSubtitle.Text     = "";
        }

        // ── Попап ввода оценки ─────────────────────────────────────────────
        private void BuildGradePopup()
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6) };

            foreach (var val in new int?[] { null, 2, 3, 4, 5 })
            {
                var btn = new Button
                {
                    Content         = val?.ToString() ?? "—",
                    Tag             = val,
                    Width           = 38,
                    Height          = 38,
                    Margin          = new Thickness(2),
                    FontSize        = 14,
                    FontWeight      = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor          = Cursors.Hand
                };
                if (val == null)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(243, 242, 241));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(50, 49, 48));
                }
                else
                {
                    btn.Background = GradeBackConv.BrushForGrade(val.Value);
                    btn.Foreground = Brushes.White;
                }
                btn.Click += GradeBtn_Click;
                sp.Children.Add(btn);
            }

            var border = new Border
            {
                Background      = Brushes.White,
                BorderBrush     = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(8),
                Child           = sp,
                Effect          = new DropShadowEffect
                {
                    Color       = Colors.Black, Opacity = 0.15,
                    BlurRadius  = 12, ShadowDepth = 3, Direction = 270
                }
            };

            _gradePopup = new Popup
            {
                Child              = border,
                StaysOpen          = true,   // закрываем вручную через Win_PreviewMouseDown
                AllowsTransparency = true,
                PopupAnimation     = PopupAnimation.Fade,
                Placement          = PlacementMode.MousePoint,
                Focusable          = false
            };
        }

        private void GradeBtn_Click(object sender, RoutedEventArgs e)
        {
            _gradePopup.IsOpen = false;
            int? grade = (sender as Button)?.Tag as int?;
            if (_popDate.HasValue)
                SaveGrade(_popStudentId, _popSubjectId, _popDate.Value, grade);
            else
                SaveMonthlyGrade(_popStudentId, _popSubjectId, grade);
        }

        private void MainGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!CanEdit) return;

            var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null) return;

            string tag = cell.Column != null && _colTags.TryGetValue(cell.Column, out string ct) ? ct : "";
            if (tag == "name" || tag == "avg" || string.IsNullOrEmpty(tag)) return;

            var drv = cell.DataContext as DataRowView;
            if (drv == null) return;

            _popStudentId = Convert.ToInt32(drv["StudentId"]);
            _popSubjectId = _currentSubjectId;
            _popDate      = null;

            if (tag.StartsWith("day:"))
            {
                // Журнал: дневная оценка — предмет уже выбран в CmbSubject
                int day  = int.Parse(tag.Substring(4));
                _popDate = new DateTime(_year, _month, day);
            }
            else if (tag.StartsWith("subj:"))
            {
                // Ведомость: проверяем, что этот предмет входит в редактируемые
                int subjectId = int.Parse(tag.Substring(5));
                if (!_editableSubjectIds.Contains(subjectId)) return; // чужой предмет
                _popSubjectId = subjectId;
                // _popDate = null → итоговая за месяц
            }
            // "final" → итоговая по текущему предмету журнала

            // Закрываем старый попап и открываем на новой позиции
            _gradePopup.IsOpen = false;
            _gradePopup.IsOpen = true;

            e.Handled = true;
        }

        // ── Сохранение оценок ─────────────────────────────────────────────
        private void SaveGrade(int studentId, int subjectId, DateTime date, int? value)
        {
            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_SetGrade", new[]
                {
                    new SqlParameter("@StudentId",  studentId),
                    new SqlParameter("@SubjectId",  subjectId),
                    new SqlParameter("@AddedById",  SessionHelper.UserId),
                    new SqlParameter("@GradeDate",  date.Date),
                    new SqlParameter("@GradeValue", (object)value ?? DBNull.Value)
                });
                LoadJournal();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveMonthlyGrade(int studentId, int subjectId, int? value)
        {
            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_SetMonthlyGrade", new[]
                {
                    new SqlParameter("@StudentId",   studentId),
                    new SqlParameter("@SubjectId",   subjectId),
                    new SqlParameter("@Year",        _year),
                    new SqlParameter("@Month",       _month),
                    new SqlParameter("@FinalGrade",  (object)value ?? DBNull.Value),
                    new SqlParameter("@UpdatedById", SessionHelper.UserId)
                });
                if (_isJournalView) LoadJournal(); else LoadStatement();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Экспорт ────────────────────────────────────────────────────────
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var view = MainGrid.ItemsSource as DataView;
            if (view == null || view.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string mode  = _isJournalView ? "Журнал" : "Ведомость";
            string month = new DateTime(_year, _month, 1)
                .ToString("MMMM_yyyy", new CultureInfo("ru-RU"));

            var dlg = new SaveFileDialog
            {
                Title    = "Сохранить",
                Filter   = "Excel|*.xlsx",
                FileName = $"{mode}_{month}"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add(mode);
                    ws.Style.Font.FontName = "Arial";
                    ws.Style.Font.FontSize = 10;

                    var dt = view.Table;

                    // Заголовок
                    ws.Cell(1, 1).Value = $"{mode} — " +
                        new DateTime(_year, _month, 1).ToString("MMMM yyyy", new CultureInfo("ru-RU"));
                    ws.Cell(1, 1).Style.Font.Bold     = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 13;

                    // Шапка колонок
                    int exCol = 1;
                    var colHeaders = new List<(int dtIndex, string header)>();

                    // Строим соответствие DataTable-колонок и заголовков из DataGrid
                    int gridColIdx = 0;
                    for (int c = 0; c < dt.Columns.Count; c++)
                    {
                        if (dt.Columns[c].ColumnName == "StudentId") continue;

                        string hdr = dt.Columns[c].ColumnName;
                        if (gridColIdx < MainGrid.Columns.Count)
                        {
                            var gc = MainGrid.Columns[gridColIdx];
                            hdr = gc.Header is string s   ? s
                                : gc.Header is TextBlock tb ? tb.Text
                                : hdr;
                        }
                        gridColIdx++;

                        colHeaders.Add((c, hdr));
                        var xlCell = ws.Cell(3, exCol);
                        xlCell.Value = hdr;
                        xlCell.Style.Font.Bold                = true;
                        xlCell.Style.Fill.BackgroundColor     = XLColor.FromArgb(0, 120, 212);
                        xlCell.Style.Font.FontColor           = XLColor.White;
                        xlCell.Style.Alignment.Horizontal     = XLAlignmentHorizontalValues.Center;
                        exCol++;
                    }

                    // Данные
                    int exRow = 4;
                    foreach (DataRowView drv in view)
                    {
                        exCol = 1;
                        foreach (var (dtIndex, _) in colHeaders)
                            ws.Cell(exRow, exCol++).Value = drv[dtIndex]?.ToString() ?? "";
                        exRow++;
                    }

                    ws.Columns().AdjustToContents();
                    wb.SaveAs(dlg.FileName);
                }
                if (MessageBox.Show("Открыть файл?", "Готово",
                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Утилиты ────────────────────────────────────────────────────────
        private static T FindParent<T>(DependencyObject o) where T : DependencyObject
        {
            while (o != null)
            {
                if (o is T t) return t;
                o = VisualTreeHelper.GetParent(o);
            }
            return null;
        }
    }

    // ── Конвертеры оценок ──────────────────────────────────────────────────

    public class GradeBackConv : IValueConverter
    {
        public static readonly GradeBackConv Instance = new GradeBackConv();

        public static SolidColorBrush BrushForGrade(int g)
        {
            switch (g)
            {
                case 5: return new SolidColorBrush(Color.FromRgb(16,  124, 16));
                case 4: return new SolidColorBrush(Color.FromRgb(0,   120, 212));
                case 3: return new SolidColorBrush(Color.FromRgb(202, 80,  16));
                case 2: return new SolidColorBrush(Color.FromRgb(196, 43,  28));
                default: return Brushes.Transparent as SolidColorBrush;
            }
        }

        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            string s = value?.ToString()?.TrimEnd('*') ?? "";
            return int.TryParse(s, out int g) && g >= 2 && g <= 5
                ? (object)BrushForGrade(g)
                : Brushes.Transparent;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class GradeForeConv : IValueConverter
    {
        public static readonly GradeForeConv Instance = new GradeForeConv();
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            string s = value?.ToString()?.TrimEnd('*') ?? "";
            return int.TryParse(s, out int g) && g >= 2 && g <= 5
                ? (object)Brushes.White
                : Brushes.Transparent;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class GradeTextConv : IValueConverter
    {
        public static readonly GradeTextConv Instance = new GradeTextConv();
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value?.ToString() ?? "";
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Вспомогательный класс (локальный, не конфликтует с IdName в Announcements) ──
    internal class IdNameG
    {
        public int    Id   { get; }
        public string Name { get; }
        public IdNameG(int id, string name) { Id = id; Name = name; }
    }

    // Оставляем GradeRow для совместимости
    public class GradeRow
    {
        public string StudentName { get; set; }
        public string Subject     { get; set; }
        public string GradeType   { get; set; }
        public string GradeValue  { get; set; }
        public string GradeDate   { get; set; }
        public string GradeColor  { get; set; }
    }
}
