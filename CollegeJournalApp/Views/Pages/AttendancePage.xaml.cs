using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ClosedXML.Excel;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views.Dialogs;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CollegeJournalApp.Views.Pages
{
    public partial class AttendancePage : Page
    {
        private List<AttRow> _all            = new List<AttRow>();
        private List<AttRow> _filtered       = new List<AttRow>();
        private bool   _filterSuspended      = false;
        private bool   _initialized          = false;
        private string _activeStatusFilter   = null;
        private int    _currentPage          = 1;
        private int    _pageSize             = 25;

        public AttendancePage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => Init();
        }

        // ── Инициализация ──────────────────────────────────────────────────

        private void Init()
        {
            ConfigureForRole();
            LoadData();
            _initialized = true;
        }

        private void ConfigureForRole()
        {
            var role = SessionHelper.RoleName;

            // Кнопка «Отметить посещаемость» — Teacher, Headman, Admin
            if (role == "Teacher" || role == "Headman" || role == "Admin")
                BtnMarkAttendance.Visibility = Visibility.Visible;

            // Student → только личная статистика
            if (role == "Student")
            {
                PanelGrid.Visibility    = Visibility.Collapsed;
                PanelFilters.Visibility = Visibility.Collapsed;
                PanelStudent.Visibility = Visibility.Visible;
                return;
            }

            // Куратор и Headman — одна группа, колонка «Группа» лишняя
            if (role == "Curator" || role == "Headman")
                ColGroup.Visibility = Visibility.Collapsed;

            // Admin — показываем кнопки действий и фильтр по группе
            if (role == "Admin")
            {
                ColActions.Visibility = Visibility.Visible;
                CmbGroup.Visibility   = Visibility.Visible;
            }
        }

        // ── Загрузка данных ────────────────────────────────────────────────

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAttendanceReport", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });

                _all.Clear();
                var subjects = new HashSet<string>();
                var groups   = new HashSet<string>();

                foreach (DataRow r in dt.Rows)
                {
                    var status  = r["Status"]?.ToString()      ?? "—";
                    var subject = r["SubjectName"]?.ToString() ?? "—";
                    var group   = r["GroupName"]?.ToString()   ?? "—";
                    subjects.Add(subject);
                    groups.Add(group);

                    var dateRaw = r["LessonDate"] != DBNull.Value
                        ? Convert.ToDateTime(r["LessonDate"])
                        : DateTime.MinValue;

                    _all.Add(new AttRow
                    {
                        AttendanceId  = r["AttendanceId"] != DBNull.Value
                                        ? (int?)Convert.ToInt32(r["AttendanceId"]) : null,
                        LessonDateRaw = dateRaw,
                        LessonDate    = dateRaw != DateTime.MinValue
                                        ? dateRaw.ToString("dd.MM.yyyy") : "—",
                        StudentName   = r["StudentName"]?.ToString()   ?? "—",
                        GroupName     = group,
                        Subject       = subject,
                        Status        = status,
                        Reason        = r["Reason"]?.ToString()        ?? "",
                        StatusBg      = GetStatusBg(status),
                        StatusFg      = GetStatusFg(status),
                        StudentId     = r["StudentId"]  != DBNull.Value
                                        ? Convert.ToInt32(r["StudentId"])  : 0,
                        ScheduleId    = r["ScheduleId"] != DBNull.Value
                                        ? Convert.ToInt32(r["ScheduleId"]) : 0
                    });
                }

                // Заполнить ComboBox дисциплин (сохранить текущий выбор)
                RefillCombo(CmbSubject, subjects.OrderBy(x => x), "Все дисциплины");

                // Заполнить ComboBox групп для Admin
                if (SessionHelper.IsAdmin)
                    RefillCombo(CmbGroup, groups.OrderBy(x => x), "Все группы");

                if (SessionHelper.IsStudent)
                    BuildStudentView();
                else
                    ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefillCombo(ComboBox cmb, IEnumerable<string> items, string allLabel)
        {
            var current = (cmb.SelectedItem as ComboBoxItem)?.Content?.ToString();
            cmb.SelectionChanged -= Filter_Changed;
            cmb.Items.Clear();
            cmb.Items.Add(new ComboBoxItem { Content = allLabel });
            foreach (var s in items)
                cmb.Items.Add(new ComboBoxItem { Content = s });
            cmb.SelectedIndex = 0;
            if (current != null && current != allLabel)
            {
                for (int i = 1; i < cmb.Items.Count; i++)
                    if ((cmb.Items[i] as ComboBoxItem)?.Content?.ToString() == current)
                    { cmb.SelectedIndex = i; break; }
            }
            cmb.SelectionChanged += Filter_Changed;
        }

        // ── Фильтрация ─────────────────────────────────────────────────────

        private void ApplyFilter()
        {
            if (_filterSuspended) return;

            var filtered = _all.AsEnumerable();

            // Поиск по имени / дисциплине
            var search = TxtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(r =>
                    r.StudentName.ToLower().Contains(search) ||
                    r.Subject.ToLower().Contains(search));

            // Диапазон дат
            if (DtFrom.SelectedDate.HasValue)
                filtered = filtered.Where(r =>
                    r.LessonDateRaw != DateTime.MinValue &&
                    r.LessonDateRaw.Date >= DtFrom.SelectedDate.Value.Date);

            if (DtTo.SelectedDate.HasValue)
                filtered = filtered.Where(r =>
                    r.LessonDateRaw != DateTime.MinValue &&
                    r.LessonDateRaw.Date <= DtTo.SelectedDate.Value.Date);

            // Статус — через карточки
            if (!string.IsNullOrEmpty(_activeStatusFilter))
                filtered = filtered.Where(r => r.Status == _activeStatusFilter);

            // Дисциплина
            if (CmbSubject.SelectedIndex > 0)
            {
                var subj = (CmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                filtered = filtered.Where(r => r.Subject == subj);
            }

            // Группа (только Admin)
            if (CmbGroup.Visibility == Visibility.Visible && CmbGroup.SelectedIndex > 0)
            {
                var grp = (CmbGroup.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                filtered = filtered.Where(r => r.GroupName == grp);
            }

            _filtered = filtered.ToList();
            TxtTotal.Text = $"— {_filtered.Count} записей";
            UpdateStats(_filtered);
            ShowPage(1);
        }

        // ── Пагинация ──────────────────────────────────────────────────────

        private void ShowPage(int page)
        {
            if (_filtered.Count == 0)
            {
                _currentPage          = 1;
                AttGrid.ItemsSource   = null;
                TxtPageInfo.Text      = "Нет записей";
                PaginationPanel.Children.Clear();
                return;
            }

            int totalPages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));
            _currentPage   = Math.Max(1, Math.Min(page, totalPages));

            int from = (_currentPage - 1) * _pageSize;
            int to   = Math.Min(from + _pageSize, _filtered.Count);

            AttGrid.ItemsSource = _filtered.Skip(from).Take(_pageSize).ToList();
            TxtPageInfo.Text    = $"Записи {from + 1}–{to} из {_filtered.Count}";

            BuildPageButtons(totalPages);
        }

        private void BuildPageButtons(int totalPages)
        {
            PaginationPanel.Children.Clear();

            // Кнопка «←»
            PaginationPanel.Children.Add(MakeNavBtn("←", _currentPage > 1,
                () => ShowPage(_currentPage - 1)));

            // Номера страниц (показываем не более 7 кнопок)
            var pages = GetPageNumbers(_currentPage, totalPages);
            int? prev = null;
            foreach (int p in pages)
            {
                if (prev.HasValue && p - prev.Value > 1)
                {
                    // Многоточие
                    PaginationPanel.Children.Add(new TextBlock
                    {
                        Text              = "…",
                        FontSize          = 12,
                        Foreground        = new SolidColorBrush(Color.FromRgb(160, 159, 157)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin            = new Thickness(2, 0, 2, 0)
                    });
                }
                int captured = p;
                bool isCurrent = p == _currentPage;
                var btn = new Button
                {
                    Content         = p.ToString(),
                    Width           = 30,
                    Height          = 28,
                    FontSize        = 12,
                    Margin          = new Thickness(2, 0, 2, 0),
                    Cursor          = System.Windows.Input.Cursors.Hand,
                    BorderThickness = new Thickness(1),
                    FontWeight      = isCurrent ? FontWeights.SemiBold : FontWeights.Normal,
                    Background      = isCurrent
                        ? new SolidColorBrush(Color.FromRgb(0, 120, 212))
                        : Brushes.White,
                    Foreground      = isCurrent
                        ? Brushes.White
                        : new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    BorderBrush     = isCurrent
                        ? new SolidColorBrush(Color.FromRgb(0, 120, 212))
                        : new SolidColorBrush(Color.FromRgb(208, 208, 208))
                };
                btn.Click += (s, e) => ShowPage(captured);
                PaginationPanel.Children.Add(btn);
                prev = p;
            }

            // Кнопка «→»
            PaginationPanel.Children.Add(MakeNavBtn("→", _currentPage < totalPages,
                () => ShowPage(_currentPage + 1)));
        }

        private static Button MakeNavBtn(string label, bool enabled, Action onClick)
        {
            var btn = new Button
            {
                Content         = label,
                Width           = 30,
                Height          = 28,
                FontSize        = 13,
                Margin          = new Thickness(2, 0, 2, 0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(1),
                IsEnabled       = enabled,
                Background      = Brushes.White,
                Foreground      = enabled
                    ? new SolidColorBrush(Color.FromRgb(0, 120, 212))
                    : new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(208, 208, 208))
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        // Алгоритм «окна» страниц: всегда показываем первую, последнюю и ±2 от текущей
        private static List<int> GetPageNumbers(int current, int total)
        {
            var set = new SortedSet<int>();
            set.Add(1);
            set.Add(total);
            for (int i = Math.Max(1, current - 2); i <= Math.Min(total, current + 2); i++)
                set.Add(i);
            return new List<int>(set);
        }

        private void CmbPageSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized) return;
            var item = CmbPageSize.SelectedItem as ComboBoxItem;
            if (item != null && int.TryParse(item.Content?.ToString(), out int size))
            {
                _pageSize = size;
                ShowPage(1);
            }
        }

        // ── Статистика ─────────────────────────────────────────────────────

        private void UpdateStats(List<AttRow> rows)
        {
            int total   = rows.Count;
            int present = rows.Count(r => r.Status == "Присутствовал");
            int absent  = rows.Count(r => r.Status == "Отсутствовал");
            int late    = rows.Count(r => r.Status == "Опоздал");
            int excused = rows.Count(r => r.Status == "Уважительная причина");

            StatPresent.Text = present.ToString();
            StatAbsent.Text  = absent.ToString();
            StatLate.Text    = late.ToString();
            StatExcused.Text = excused.ToString();
            StatPercent.Text = total > 0
                ? $"{Math.Round(100.0 * present / total, 1)}%"
                : "—";

            // Ширины прогресс-баров после отрисовки
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                double w = BarPresent.ActualWidth;
                if (w <= 0) return;
                BarPresentFill.Width = total > 0 ? w * present / total : 0;
                BarAbsentFill.Width  = total > 0 ? w * absent  / total : 0;
                BarLateFill.Width    = total > 0 ? w * late    / total : 0;
                BarExcusedFill.Width = total > 0 ? w * excused / total : 0;
                BarPercentFill.Width = total > 0 ? w * present / total : 0;
            }));
        }

        // ── Личная статистика студента ─────────────────────────────────────

        private void BuildStudentView()
        {
            TxtStudentGreeting.Text = $"Ваша посещаемость, {SessionHelper.FullName ?? "студент"}";
            var groupNames = _all.Select(r => r.GroupName).Distinct().ToList();
            TxtStudentGroup.Text = groupNames.Count > 0
                ? "Группа: " + string.Join(", ", groupNames)
                : "";

            TxtTotal.Text = $"— {_all.Count} записей";
            UpdateStats(_all);
            StudentGrid.ItemsSource = _all;

            // Полосы прогресса по дисциплинам
            SubjectBars.Children.Clear();
            var bySubject = _all
                .GroupBy(r => r.Subject)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var grp in bySubject)
            {
                int total   = grp.Count();
                int present = grp.Count(r => r.Status == "Присутствовал");
                double pct  = total > 0 ? 100.0 * present / total : 0;

                var barColor = pct >= 80 ? Color.FromRgb(16, 124, 16)
                             : pct >= 60 ? Color.FromRgb(202, 80, 16)
                             :             Color.FromRgb(209, 52, 56);

                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

                // Название дисциплины
                var lblSubj = new TextBlock
                {
                    Text              = grp.Key,
                    FontSize          = 12,
                    Foreground        = new SolidColorBrush(Color.FromRgb(31, 31, 31)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming      = TextTrimming.CharacterEllipsis,
                    ToolTip           = grp.Key
                };
                Grid.SetColumn(lblSubj, 0);
                row.Children.Add(lblSubj);

                // Полоска прогресса
                var track = new Border
                {
                    Height            = 8,
                    Background        = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                    Margin            = new Thickness(8, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var fill = new Border
                {
                    Background          = new SolidColorBrush(barColor),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Height              = 8,
                    Width               = 0
                };
                var trackGrid = new Grid();
                trackGrid.Children.Add(track);
                trackGrid.Children.Add(fill);
                Grid.SetColumn(trackGrid, 1);
                row.Children.Add(trackGrid);

                // Задать ширину после рендеринга
                double capturedPct = pct;
                fill.Loaded += (s, e2) =>
                    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                    {
                        if (track.ActualWidth > 0)
                            fill.Width = track.ActualWidth * capturedPct / 100.0;
                    }));

                // Процент
                var lblPct = new TextBlock
                {
                    Text              = $"{Math.Round(pct)}%",
                    FontSize          = 12,
                    FontWeight        = FontWeights.SemiBold,
                    Foreground        = new SolidColorBrush(barColor),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment     = TextAlignment.Right
                };
                Grid.SetColumn(lblPct, 2);
                row.Children.Add(lblPct);

                SubjectBars.Children.Add(row);
            }
        }

        // ── Обработчики фильтров ────────────────────────────────────────────

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            if (!SessionHelper.IsStudent)
                ApplyFilter();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _filterSuspended    = true;
            _activeStatusFilter = null;
            TxtSearch.Text           = "";
            DtFrom.SelectedDate      = null;
            DtTo.SelectedDate        = null;
            CmbStatus.SelectedIndex  = 0;
            CmbSubject.SelectedIndex = 0;
            if (CmbGroup.Visibility == Visibility.Visible)
                CmbGroup.SelectedIndex = 0;
            _filterSuspended = false;
            UpdateCardHighlights();
            ApplyFilter();
        }

        // ── Клики по карточкам статуса ─────────────────────────────────────

        private void CardPresent_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => ToggleCardFilter("Присутствовал");

        private void CardAbsent_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => ToggleCardFilter("Отсутствовал");

        private void CardLate_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => ToggleCardFilter("Опоздал");

        private void CardExcused_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => ToggleCardFilter("Уважительная причина");

        private void ToggleCardFilter(string status)
        {
            // Повторный клик снимает фильтр
            _activeStatusFilter = _activeStatusFilter == status ? null : status;
            UpdateCardHighlights();
            if (SessionHelper.IsStudent)
                BuildStudentView();
            else
                ApplyFilter();
        }

        private void UpdateCardHighlights()
        {
            SetCardHighlight(CardPresent, "Присутствовал",        "#107C10", "#E8F6E8");
            SetCardHighlight(CardAbsent,  "Отсутствовал",         "#D13438", "#FDE8E8");
            SetCardHighlight(CardLate,    "Опоздал",              "#CA5010", "#FDF0E8");
            SetCardHighlight(CardExcused, "Уважительная причина", "#0078D4", "#E8F0FD");
        }

        private void SetCardHighlight(Border card, string status, string colorHex, string bgHex)
        {
            bool active = _activeStatusFilter == status;
            var  color  = (Color)ColorConverter.ConvertFromString(colorHex);
            card.BorderBrush     = active
                ? new SolidColorBrush(color)
                : new SolidColorBrush(Color.FromRgb(224, 224, 224));
            card.BorderThickness = active ? new Thickness(2) : new Thickness(1);
            card.Background      = active
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex))
                : Brushes.White;
        }

        // ── Открытие окна отметки посещаемости ────────────────────────────

        private void BtnMarkAttendance_Click(object sender, RoutedEventArgs e)
        {
            var win = new MarkAttendanceWindow(SessionHelper.UserId, SessionHelper.RoleName)
            {
                Owner = Window.GetWindow(this)
            };
            if (win.ShowDialog() == true)
                LoadData();
        }

        // ── Редактирование записи (Admin) ──────────────────────────────────

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            int? attId = btn.Tag is int i ? (int?)i : null;
            if (attId == null) return;
            var row = _all.FirstOrDefault(r => r.AttendanceId == attId);
            if (row == null) return;
            ShowEditDialog(row);
        }

        private void ShowEditDialog(AttRow row)
        {
            var win = new Window
            {
                Title                 = "Изменить запись посещаемости",
                Width                 = 420,
                Height                = 310,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner                 = Window.GetWindow(this),
                ResizeMode            = ResizeMode.NoResize,
                FontFamily            = new FontFamily("Segoe UI"),
                Background            = new SolidColorBrush(Color.FromRgb(250, 249, 248))
            };

            var root = new Grid { Margin = new Thickness(20) };
            for (int i = 0; i < 6; i++)
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions[5] = new RowDefinition { Height = GridLength.Auto };

            // Заголовок — имя студента
            root.Children.Add(Rowed(0, new TextBlock
            {
                Text       = row.StudentName,
                FontSize   = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 31, 31)),
                Margin     = new Thickness(0, 0, 0, 4)
            }));

            // Подзаголовок — предмет и дата
            root.Children.Add(Rowed(1, new TextBlock
            {
                Text       = $"{row.Subject}  ·  {row.LessonDate}",
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                Margin     = new Thickness(0, 0, 0, 14)
            }));

            // Статус
            root.Children.Add(Rowed(2, new TextBlock
            {
                Text       = "Статус",
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                Margin     = new Thickness(0, 0, 0, 4)
            }));

            var statuses = new[] { "Присутствовал", "Отсутствовал", "Опоздал", "Уважительная причина" };
            var cmbSt = new ComboBox { Height = 30, FontSize = 12, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var s in statuses) cmbSt.Items.Add(new ComboBoxItem { Content = s });
            cmbSt.SelectedIndex = Math.Max(0, Array.IndexOf(statuses, row.Status));
            root.Children.Add(Rowed(3, cmbSt));

            root.Children.Add(Rowed(4, new TextBlock
            {
                Text       = "Причина (необязательно)",
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                Margin     = new Thickness(0, 0, 0, 4)
            }));

            var txtR = new TextBox
            {
                Height                      = 56,
                FontSize                    = 12,
                Text                        = row.Reason,
                Padding                     = new Thickness(8, 6, 8, 6),
                TextWrapping                = TextWrapping.Wrap,
                BorderBrush                 = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin                      = new Thickness(0, 0, 0, 14)
            };
            root.Children.Add(Rowed(5, txtR));

            // Кнопки
            var btnPanel = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnCancel = new Button
            {
                Content         = "Отмена",
                Height          = 32,
                Padding         = new Thickness(16, 0, 16, 0),
                Background      = Brushes.Transparent,
                BorderBrush     = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                BorderThickness = new Thickness(1),
                FontSize        = 12,
                Foreground      = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                Margin          = new Thickness(0, 0, 8, 0),
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s2, e2) => { win.DialogResult = false; win.Close(); };
            btnPanel.Children.Add(btnCancel);

            var btnSave = new Button
            {
                Content         = "Сохранить",
                Height          = 32,
                Padding         = new Thickness(16, 0, 16, 0),
                Background      = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                Foreground      = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize        = 12,
                FontWeight      = FontWeights.SemiBold,
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            btnSave.Click += (s2, e2) =>
            {
                var newStatus = (cmbSt.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? row.Status;
                var newReason = txtR.Text.Trim();
                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_SaveAttendanceMark", new[]
                    {
                        new SqlParameter("@MarkedById", SessionHelper.UserId),
                        new SqlParameter("@StudentId",  row.StudentId),
                        new SqlParameter("@ScheduleId", row.ScheduleId),
                        new SqlParameter("@LessonDate", row.LessonDateRaw.Date),
                        new SqlParameter("@Status",     newStatus),
                        new SqlParameter("@Reason",     string.IsNullOrEmpty(newReason)
                                                        ? (object)DBNull.Value : newReason)
                    });
                    win.DialogResult = true;
                    win.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения:\n" + ex.Message,
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnPanel.Children.Add(btnSave);

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.Children.Add(Rowed(6, btnPanel));

            win.Content = root;
            if (win.ShowDialog() == true)
                LoadData();
        }

        private static UIElement Rowed(int row, UIElement el)
        {
            Grid.SetRow(el, row);
            return el;
        }

        // ── Удаление записи (Admin) ────────────────────────────────────────

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            int? attId = btn.Tag is int i ? (int?)i : null;
            if (attId == null) return;

            var row  = _all.FirstOrDefault(r => r.AttendanceId == attId);
            var name = row != null ? $"{row.StudentName} ({row.LessonDate})" : "эту запись";

            if (MessageBox.Show($"Удалить запись посещаемости\n«{name}»?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_DeleteAttendanceMark", new[]
                {
                    new SqlParameter("@AttendanceId", attId.Value),
                    new SqlParameter("@UserId",       SessionHelper.UserId)
                });
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Экспорт в Excel ────────────────────────────────────────────────

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // Экспортируем все отфильтрованные записи, а не только текущую страницу
            var source = SessionHelper.IsStudent
                ? StudentGrid.ItemsSource as List<AttRow>
                : _filtered;

            if (source == null || source.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title    = "Сохранить посещаемость",
                Filter   = "Excel (*.xlsx)|*.xlsx",
                FileName = $"Посещаемость_{DateTime.Now:yyyy-MM-dd}"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Посещаемость");
                    ws.Style.Font.FontName = "Calibri";
                    ws.Style.Font.FontSize = 11;

                    ws.Cell(1, 1).Value = "Посещаемость";
                    ws.Cell(1, 1).Style.Font.Bold     = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 14;
                    ws.Cell(2, 1).Value = $"Экспорт: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

                    bool isAdmin = SessionHelper.IsAdmin;
                    string[] headers = SessionHelper.IsStudent
                        ? new[] { "Дата", "Дисциплина", "Статус", "Причина" }
                        : isAdmin
                          ? new[] { "Дата", "Студент", "Группа", "Дисциплина", "Статус", "Причина" }
                          : new[] { "Дата", "Студент", "Дисциплина", "Статус", "Причина" };

                    for (int c = 0; c < headers.Length; c++)
                    {
                        var cell = ws.Cell(4, c + 1);
                        cell.Value = headers[c];
                        cell.Style.Font.Bold                = true;
                        cell.Style.Fill.BackgroundColor     = XLColor.FromArgb(0, 120, 212);
                        cell.Style.Font.FontColor           = XLColor.White;
                    }

                    var statusColors = new Dictionary<string, XLColor>
                    {
                        { "Присутствовал",        XLColor.FromArgb(232, 246, 232) },
                        { "Отсутствовал",         XLColor.FromArgb(253, 232, 233) },
                        { "Опоздал",              XLColor.FromArgb(253, 240, 232) },
                        { "Уважительная причина", XLColor.FromArgb(232, 240, 253) }
                    };

                    for (int i = 0; i < source.Count; i++)
                    {
                        var r   = source[i];
                        int row = 5 + i;

                        if (SessionHelper.IsStudent)
                        {
                            ws.Cell(row, 1).Value = r.LessonDate;
                            ws.Cell(row, 2).Value = r.Subject;
                            ws.Cell(row, 3).Value = r.Status;
                            ws.Cell(row, 4).Value = r.Reason;
                        }
                        else if (isAdmin)
                        {
                            ws.Cell(row, 1).Value = r.LessonDate;
                            ws.Cell(row, 2).Value = r.StudentName;
                            ws.Cell(row, 3).Value = r.GroupName;
                            ws.Cell(row, 4).Value = r.Subject;
                            ws.Cell(row, 5).Value = r.Status;
                            ws.Cell(row, 6).Value = r.Reason;
                        }
                        else
                        {
                            ws.Cell(row, 1).Value = r.LessonDate;
                            ws.Cell(row, 2).Value = r.StudentName;
                            ws.Cell(row, 3).Value = r.Subject;
                            ws.Cell(row, 4).Value = r.Status;
                            ws.Cell(row, 5).Value = r.Reason;
                        }

                        if (statusColors.TryGetValue(r.Status, out var bg))
                            ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = bg;
                    }

                    ws.Column(1).Width = 12;
                    ws.Column(2).Width = 28;
                    ws.Column(3).Width = isAdmin ? 12 : 28;
                    ws.Column(4).Width = isAdmin ? 28 : 14;
                    if (headers.Length >= 5) ws.Column(5).Width = 14;
                    if (headers.Length >= 6) ws.Column(6).Width = 25;

                    wb.SaveAs(dlg.FileName);
                }

                if (MessageBox.Show("Файл сохранён. Открыть?", "Готово",
                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Цвета статус-бейджей ───────────────────────────────────────────

        private static string GetStatusBg(string s)
        {
            switch (s)
            {
                case "Присутствовал":        return "#E8F6E8";
                case "Отсутствовал":         return "#FDE8E8";
                case "Опоздал":              return "#FDF0E8";
                case "Уважительная причина": return "#E8F0FD";
                default:                     return "#F0F0F0";
            }
        }

        private static string GetStatusFg(string s)
        {
            switch (s)
            {
                case "Присутствовал":        return "#107C10";
                case "Отсутствовал":         return "#A4262C";
                case "Опоздал":              return "#8A3B00";
                case "Уважительная причина": return "#0050A4";
                default:                     return "#605E5C";
            }
        }
    }

    public class AttRow
    {
        public int?     AttendanceId  { get; set; }
        public DateTime LessonDateRaw { get; set; }
        public string   LessonDate    { get; set; }
        public string   StudentName   { get; set; }
        public string   GroupName     { get; set; }
        public string   Subject       { get; set; }
        public string   Status        { get; set; }
        public string   Reason        { get; set; }
        public string   StatusBg      { get; set; }
        public string   StatusFg      { get; set; }
        public int      StudentId     { get; set; }
        public int      ScheduleId    { get; set; }
    }
}
