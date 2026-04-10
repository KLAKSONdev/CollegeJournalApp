using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            TxtDate.Text = DateTime.Now.ToString("dd MMMM yyyy", new CultureInfo("ru-RU"));
            LoadStats();
            LoadRecentEvents();
        }

        private void LoadStats()
        {
            // Заглушка — заменишь на реальные запросы к БД
            TxtStudentCount.Text    = "24";
            TxtAttendanceToday.Text = "78%";
            TxtAvgGrade.Text        = "4.1";
            TxtAbsentCount.Text     = "3";
        }

        private void LoadRecentEvents()
        {
            var events = new List<DashboardEvent>();

            try
            {
                // Получаем последние записи из журнала аудита
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetRecentAudit",
                    new[] { new SqlParameter("@Limit", 6) });

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    events.Add(new DashboardEvent
                    {
                        Text     = row["ReadableAction"]?.ToString() ?? "",
                        Time     = Convert.ToDateTime(row["ActionAt"]).ToString("HH:mm"),
                        DotColor = GetDotColor(row["Action"]?.ToString())
                    });
                }
            }
            catch
            {
                // Заглушка пока процедура не создана
                events.Add(new DashboardEvent
                {
                    Text     = "Добро пожаловать в систему «Классный журнал»",
                    Time     = DateTime.Now.ToString("HH:mm"),
                    DotColor = "#c9a84c"
                });
                events.Add(new DashboardEvent
                {
                    Text     = $"Вход выполнен: {SessionHelper.FullName} ({GetRoleRu(SessionHelper.RoleName)})",
                    Time     = DateTime.Now.ToString("HH:mm"),
                    DotColor = "#1a6b3c"
                });
            }

            EventsList.ItemsSource = events;
        }

        private string GetDotColor(string action)
        {
            switch (action)
            {
                case "LOGIN":       return "#1a6b3c";
                case "CREATE":      return "#1a6b3c";
                case "SOFT_DELETE": return "#c0392b";
                case "VIEW":        return "#c9a84c";
                default:            return "#6b7a99";
            }
        }

        private string GetRoleRu(string role)
        {
            switch (role)
            {
                case "Admin":   return "Администратор";
                case "Curator": return "Куратор";
                case "Headman": return "Староста";
                case "Student": return "Студент";
                default:        return role ?? "";
            }
        }
    }

    public class DashboardEvent
    {
        public string Text     { get; set; }
        public string Time     { get; set; }
        public string DotColor { get; set; }
    }
}
