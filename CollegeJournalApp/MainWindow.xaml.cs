using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views.Dialogs;
using CollegeJournalApp.Views.Pages;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp
{
    public partial class MainWindow : Window
    {
        private Button          _activeTab;
        private List<NotifItem> _notifs    = new List<NotifItem>();
        private int             _lastSeenId = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtUserName.Text = SessionHelper.FullName;
            TxtRole.Text     = GetRoleDisplayName(SessionHelper.RoleName);

            if (SessionHelper.IsAdmin)
            {
                TabAudit.Visibility  = Visibility.Visible;
                TabUsers.Visibility  = Visibility.Visible;
                TabAdmin.Visibility  = Visibility.Visible;
            }

            if (SessionHelper.IsTeacher)
            {
                TabStudents.Visibility     = Visibility.Collapsed;
                TabSocial.Visibility       = Visibility.Collapsed;
                TabAchievements.Visibility = Visibility.Collapsed;
                // TabGrades доступен преподавателям — видят свои группы и предметы
            }

            if (SessionHelper.IsStudent)
                TabSocial.Visibility = Visibility.Collapsed;

            _activeTab = TabDashboard;
            NavigateTo("Dashboard");

            _lastSeenId = LoadLastSeenId();
            LoadNotifications();
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (_activeTab != null)
                    _activeTab.Style = (Style)FindResource("TabStyle");
                btn.Style  = (Style)FindResource("TabActiveStyle");
                _activeTab = btn;
                NavigateTo(btn.Tag?.ToString());
            }
        }

        private void NavigateTo(string page)
        {
            switch (page)
            {
                case "Dashboard":     MainFrame.Navigate(new DashboardPage());     break;
                case "Students":      MainFrame.Navigate(new StudentsPage());      break;
                case "Attendance":    MainFrame.Navigate(new AttendancePage());    break;
                case "Grades":        MainFrame.Navigate(new GradesPage());        break;
                case "Schedule":      MainFrame.Navigate(new SchedulePage());      break;
                case "Events":        MainFrame.Navigate(new EventsPage());        break;
                case "Announcements": MainFrame.Navigate(new AnnouncementsPage()); break;
                case "Assignments":   MainFrame.Navigate(new AssignmentsPage());   break;
                case "Documents":     MainFrame.Navigate(new DocumentsPage());     break;
                case "Social":        MainFrame.Navigate(new SocialPage());        break;
                case "Achievements":  MainFrame.Navigate(new AchievementsPage());  break;
                case "Audit":         MainFrame.Navigate(new AuditPage());         break;
                case "Users":         MainFrame.Navigate(new UsersPage());         break;
                case "Admin":         MainFrame.Navigate(new AdminPage());         break;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.F1) return;
            e.Handled = true;

            // Определяем текущую страницу для открытия нужного раздела справки
            var pageName = (MainFrame.Content?.GetType().Name) ?? "";
            new HelpWindow(pageName, this).ShowDialog();
        }

        // ── Управление окном ───────────────────────────────────────────────

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState          = WindowState.Normal;
                BtnMaximize.Content  = "□";
                BtnMaximize.ToolTip  = "Развернуть";
            }
            else
            {
                WindowState          = WindowState.Maximized;
                BtnMaximize.Content  = "❐";
                BtnMaximize.ToolTip  = "Восстановить";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        // ── Выход из аккаунта ──────────────────────────────────────────────

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти из системы?",
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SessionHelper.Clear();
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private string GetRoleDisplayName(string role)
        {
            switch (role)
            {
                case "Admin":   return "Администратор";
                case "Curator": return "Куратор";
                case "Headman": return "Староста";
                case "Student": return "Студент";
                case "Teacher": return "Преподаватель";
                default:        return role ?? "";
            }
        }

        // ── Уведомления ────────────────────────────────────────────────────

        private void LoadNotifications()
        {
            _notifs.Clear();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAnnouncements", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });

                foreach (DataRow r in dt.Rows)
                {
                    _notifs.Add(new NotifItem
                    {
                        AnnouncementId = Convert.ToInt32(r["AnnouncementId"]),
                        Title          = r["Title"]?.ToString() ?? "",
                        Body           = r["Body"]?.ToString()  ?? "",
                        CreatedAt      = r["CreatedAt"] != DBNull.Value
                                         ? Convert.ToDateTime(r["CreatedAt"])
                                         : DateTime.MinValue
                    });
                }
            }
            catch { }

            UpdateBadge();
        }

        private void UpdateBadge()
        {
            int newCount = _notifs.Count(n => n.AnnouncementId > _lastSeenId);
            if (newCount > 0)
            {
                NotifBadgeText.Text      = newCount > 99 ? "99+" : newCount.ToString();
                NotifBadge.Visibility    = Visibility.Visible;
            }
            else
            {
                NotifBadge.Visibility    = Visibility.Collapsed;
            }
        }

        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            BuildNotifList();
            NotifPopup.IsOpen = true;

            // Отмечаем всё как прочитанное при открытии
            MarkAllSeen();
        }

        private void BuildNotifList()
        {
            NotifList.Children.Clear();

            if (_notifs.Count == 0)
            {
                NotifList.Children.Add(new TextBlock
                {
                    Text                = "Нет объявлений",
                    FontSize            = 12,
                    Foreground          = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin              = new Thickness(0, 24, 0, 24)
                });
                return;
            }

            // Показываем последние 15
            foreach (var n in _notifs.Take(15))
            {
                bool isNew = n.AnnouncementId > _lastSeenId;
                NotifList.Children.Add(BuildNotifRow(n, isNew));
            }
        }

        private UIElement BuildNotifRow(NotifItem n, bool isNew)
        {
            var border = new Border
            {
                Background      = isNew
                    ? new SolidColorBrush(Color.FromArgb(18, 0, 120, 212))
                    : Brushes.Transparent,
                BorderBrush     = new SolidColorBrush(Color.FromRgb(235, 235, 235)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(16, 10, 16, 10),
                Cursor          = System.Windows.Input.Cursors.Hand
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Синяя точка — только для новых
            if (isNew)
            {
                var dot = new Border
                {
                    Width        = 6,
                    Height       = 6,
                    Background   = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                    CornerRadius = new CornerRadius(3),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin       = new Thickness(0, 5, 0, 0)
                };
                Grid.SetColumn(dot, 0);
                row.Children.Add(dot);
            }

            // Контент
            var content = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            Grid.SetColumn(content, 1);

            // Заголовок + дата
            var titleRow = new Grid();
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleTb = new TextBlock
            {
                Text          = n.Title,
                FontSize      = 12,
                FontWeight    = isNew ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground    = new SolidColorBrush(Color.FromRgb(26, 26, 46)),
                TextTrimming  = TextTrimming.CharacterEllipsis,
                TextWrapping  = TextWrapping.NoWrap
            };
            Grid.SetColumn(titleTb, 0);
            titleRow.Children.Add(titleTb);

            var dateTb = new TextBlock
            {
                Text       = n.CreatedAt != DateTime.MinValue
                             ? n.CreatedAt.ToString("dd MMM", new System.Globalization.CultureInfo("ru-RU"))
                             : "",
                FontSize   = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(134, 142, 150)),
                Margin     = new Thickness(8, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(dateTb, 1);
            titleRow.Children.Add(dateTb);

            content.Children.Add(titleRow);

            // Тело — обрезанное
            var snippet = n.Body.Length > 80 ? n.Body.Substring(0, 80) + "…" : n.Body;
            content.Children.Add(new TextBlock
            {
                Text       = snippet,
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                TextWrapping = TextWrapping.Wrap,
                Margin     = new Thickness(0, 3, 0, 0)
            });

            row.Children.Add(content);
            border.Child = row;

            // Клик — переходим на страницу объявлений
            border.MouseLeftButtonUp += (s, e) =>
            {
                NotifPopup.IsOpen = false;
                NavigateToAnnouncements();
            };

            // Hover-подсветка
            border.MouseEnter += (s, e) =>
                border.Background = new SolidColorBrush(Color.FromArgb(30, 0, 120, 212));
            border.MouseLeave += (s, e) =>
                border.Background = isNew
                    ? new SolidColorBrush(Color.FromArgb(18, 0, 120, 212))
                    : Brushes.Transparent;

            return border;
        }

        private void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            MarkAllSeen();
            BuildNotifList(); // перерисовать без синих точек
        }

        private void BtnShowAllAnn_Click(object sender, RoutedEventArgs e)
        {
            NotifPopup.IsOpen = false;
            NavigateToAnnouncements();
        }

        private void NavigateToAnnouncements()
        {
            // Переключаем активную вкладку
            if (_activeTab != null)
                _activeTab.Style = (Style)FindResource("TabStyle");
            TabAnnouncements.Style = (Style)FindResource("TabActiveStyle");
            _activeTab = TabAnnouncements;
            NavigateTo("Announcements");
        }

        private void MarkAllSeen()
        {
            if (_notifs.Count == 0) return;
            int maxId = _notifs.Max(n => n.AnnouncementId);
            if (maxId > _lastSeenId)
            {
                _lastSeenId = maxId;
                SaveLastSeenId(_lastSeenId);
                UpdateBadge();
            }
        }

        // ── Хранение «последнего просмотренного» ID в файле ───────────────

        private static string NotifFilePath(int userId)
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CollegeJournalApp");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"notif_{userId}.dat");
        }

        private int LoadLastSeenId()
        {
            try
            {
                var file = NotifFilePath(SessionHelper.UserId);
                if (File.Exists(file) && int.TryParse(File.ReadAllText(file).Trim(), out int id))
                    return id;
            }
            catch { }
            return 0;
        }

        private void SaveLastSeenId(int id)
        {
            try { File.WriteAllText(NotifFilePath(SessionHelper.UserId), id.ToString()); }
            catch { }
        }

        // ── Вспомогательный класс ──────────────────────────────────────────

        private class NotifItem
        {
            public int      AnnouncementId { get; set; }
            public string   Title          { get; set; }
            public string   Body           { get; set; }
            public DateTime CreatedAt      { get; set; }
        }
    }
}
