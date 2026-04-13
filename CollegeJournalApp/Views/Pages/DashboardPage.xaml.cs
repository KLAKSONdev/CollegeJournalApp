using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadData();
        }

        private void LoadData()
        {
            TxtDate.Text = DateTime.Now.ToString("dd MMMM yyyy", new CultureInfo("ru-RU"));
            LoadStats();
            LoadEvents();
        }

        private void LoadStats()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetDashboard", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });
                if (dt == null || dt.Rows.Count == 0) return;
                var row = dt.Rows[0];

                TxtStudentCount.Text    = row["StudentCount"]?.ToString() ?? "—";
                var attPct              = row["AttendancePercent"];
                TxtAttendanceToday.Text = attPct != DBNull.Value ? attPct + "%" : "нет данных";
                var avg                 = Convert.ToDecimal(row["AvgGrade"]);
                TxtAvgGrade.Text        = avg > 0 ? avg.ToString("F1") : "—";
                TxtAbsentCount.Text     = row["AbsentCount"]?.ToString() ?? "—";

                var groupName  = row["GroupName"]?.ToString() ?? "—";
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                    mainWindow.TxtGroupName.Text = groupName;
            }
            catch
            {
                TxtStudentCount.Text = TxtAttendanceToday.Text =
                TxtAvgGrade.Text     = TxtAbsentCount.Text = "—";
            }
        }

        private void LoadEvents()
        {
            var events = new List<DashboardEvent>();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetDashboardEvents", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName),
                    new SqlParameter("@Limit",    8)
                });

                bool isAdmin = SessionHelper.IsAdmin;
                TxtEventsTitle.Text     = isAdmin ? "Последние действия в системе" : "Объявления и события группы";
                TxtClickHint.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

                foreach (DataRow row in dt.Rows)
                {
                    var eventType = row["EventType"]?.ToString() ?? "";
                    int? logId = null;
                    if (row.Table.Columns.Contains("LogId") && row["LogId"] != DBNull.Value)
                        logId = Convert.ToInt32(row["LogId"]);

                    events.Add(new DashboardEvent
                    {
                        LogId    = logId,
                        Text     = row["EventText"]?.ToString() ?? "",
                        Time     = Convert.ToDateTime(row["EventTime"]).ToString("HH:mm dd.MM"),
                        DotColor = GetDotColor(eventType),
                        CanOpen  = isAdmin && logId.HasValue
                    });
                }

                if (events.Count == 0)
                    events.Add(new DashboardEvent
                    {
                        Text = "Нет актуальных событий", Time = "", DotColor = "#A19F9D"
                    });
            }
            catch
            {
                TxtEventsTitle.Text = "События";
                events.Add(new DashboardEvent
                {
                    Text = $"Добро пожаловать, {SessionHelper.FullName}",
                    Time = DateTime.Now.ToString("HH:mm"), DotColor = "#107C10"
                });
            }

            EventsList.ItemsSource = events;
        }

        private void EventsList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(EventsList.SelectedItem is DashboardEvent ev)) return;
            if (!ev.CanOpen || !ev.LogId.HasValue) return;

            var win = new AuditDetailWindow(ev.LogId.Value);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        private string GetDotColor(string eventType)
        {
            switch (eventType)
            {
                case "LOGIN":        return "#107C10";
                case "CREATE":       return "#0078D4";
                case "SOFT_DELETE":  return "#D13438";
                case "UPDATE":       return "#CA5010";
                case "VIEW":         return "#CA5010";
                case "ANNOUNCEMENT": return "#0078D4";
                case "EVENT":        return "#107C10";
                default:             return "#A19F9D";
            }
        }
    }

    public class DashboardEvent
    {
        public int?   LogId    { get; set; }
        public string Text     { get; set; }
        public string Time     { get; set; }
        public string DotColor { get; set; }
        public bool   CanOpen  { get; set; }
    }
}
