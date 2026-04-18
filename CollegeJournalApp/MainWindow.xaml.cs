using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views.Dialogs;
using CollegeJournalApp.Views.Pages;

namespace CollegeJournalApp
{
    public partial class MainWindow : Window
    {
        private Button _activeTab;

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
                // Преподаватель видит только своё расписание + общие разделы
                TabStudents.Visibility     = Visibility.Collapsed;
                TabGrades.Visibility       = Visibility.Collapsed;
                TabSocial.Visibility       = Visibility.Collapsed;
                TabAchievements.Visibility = Visibility.Collapsed;
            }

            if (SessionHelper.IsStudent)
                TabSocial.Visibility = Visibility.Collapsed;

            _activeTab = TabDashboard;
            NavigateTo("Dashboard");
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
    }
}
