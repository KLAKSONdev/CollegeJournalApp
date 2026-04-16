using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class DashboardPage : Page
    {
        private int _groupId; // кэш GroupId из sp_GetDashboard

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadAll();
        }

        // ────────────────────────────────────────────────────────────
        private void LoadAll()
        {
            var ci = new CultureInfo("ru-RU");
            TxtDate.Text = DateTime.Now.ToString("dd MMMM yyyy", ci);

            SetupRoleLayout();
            LoadStats();
            LoadEvents();
            if (!SessionHelper.IsAdmin)
                LoadTodaySchedule();
        }

        // ────────────────────────────────────────────────────────────
        //  Настройка видимости секций + тексты заголовка по роли
        // ────────────────────────────────────────────────────────────
        private void SetupRoleLayout()
        {
            if (SessionHelper.IsAdmin)
            {
                TxtGreeting.Text      = "Системный дашборд";
                TxtSubtitle.Text      = "Администратор — полный обзор системы";
                AdminStatsPanel.Visibility = Visibility.Visible;
                GroupStatsPanel.Visibility = Visibility.Collapsed;
                SchedulePanel.Visibility   = Visibility.Collapsed;
                TxtClickHint.Visibility    = Visibility.Visible;
                TxtEventsTitle.Text        = "Последние действия в системе";
            }
            else if (SessionHelper.IsCurator)
            {
                TxtGreeting.Text      = "Панель куратора";
                TxtSubtitle.Text      = "Обзор вашей группы";
                AdminStatsPanel.Visibility = Visibility.Collapsed;
                GroupStatsPanel.Visibility = Visibility.Visible;
                SchedulePanel.Visibility   = Visibility.Visible;
                TxtEventsTitle.Text        = "Последние оценки в группе";
                SetCardLabels("Студентов в группе", "#0078D4",
                              "Посещаемость сегодня", "#107C10",
                              "Средний балл",         "#107C10",
                              "Пропусков за 30 дней", "#D13438");
            }
            else if (SessionHelper.IsHeadman)
            {
                TxtGreeting.Text      = "Панель старосты";
                TxtSubtitle.Text      = "Ваша группа сегодня";
                AdminStatsPanel.Visibility = Visibility.Collapsed;
                GroupStatsPanel.Visibility = Visibility.Visible;
                SchedulePanel.Visibility   = Visibility.Visible;
                TxtEventsTitle.Text        = "Расписание на сегодня";
                SetCardLabels("Студентов в группе", "#0078D4",
                              "Присутствует сегодня", "#107C10",
                              "Отсутствует сегодня",  "#D13438",
                              "Пар сегодня",          "#CA5010");
            }
            else // Student
            {
                TxtGreeting.Text      = "Привет, " + SessionHelper.FirstName + "!";
                TxtSubtitle.Text      = "Ваша успеваемость и расписание";
                AdminStatsPanel.Visibility = Visibility.Collapsed;
                GroupStatsPanel.Visibility = Visibility.Visible;
                SchedulePanel.Visibility   = Visibility.Visible;
                TxtEventsTitle.Text        = "Мои последние оценки";
                SetCardLabels("Мой средний балл",    "#107C10",
                              "Посещаемость за месяц", "#0078D4",
                              "Пропусков за месяц",  "#D13438",
                              "Пар сегодня",          "#CA5010");
            }

            // Текущий день недели для заголовка расписания
            TxtWeekday.Text = DateTime.Now.ToString("dddd", new CultureInfo("ru-RU"));
        }

        private void SetCardLabels(
            string l1, string c1,
            string l2, string c2,
            string l3, string c3,
            string l4, string c4)
        {
            TxtLabel1.Text     = l1;
            TxtLabel2.Text     = l2;
            TxtLabel3.Text     = l3;
            TxtLabel4.Text     = l4;
            Card1Bar.Background = BrushOf(c1);
            Card2Bar.Background = BrushOf(c2);
            Card3Bar.Background = BrushOf(c3);
            Card4Bar.Background = BrushOf(c4);
        }

        private static SolidColorBrush BrushOf(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        // ────────────────────────────────────────────────────────────
        //  Статистические карточки
        // ────────────────────────────────────────────────────────────
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

                // Кэшируем GroupId для расписания
                if (dt.Columns.Contains("GroupId") && row["GroupId"] != DBNull.Value)
                    _groupId = Convert.ToInt32(row["GroupId"]);

                // Обновить название группы в шапке
                var groupName = row["GroupName"]?.ToString() ?? "—";
                var mw = Application.Current.MainWindow as MainWindow;
                if (mw != null)
                    mw.TxtGroupName.Text = groupName;

                if (SessionHelper.IsAdmin)
                {
                    TxtAdminUsers.Text   = Fmt(row["UserCount"]);
                    TxtAdminStudents.Text = Fmt(row["StudentCount"]);
                    TxtAdminGroups.Text  = Fmt(row["GroupCount"]);

                    var avg = ToDecimal(row["AvgGrade"]);
                    TxtAdminAvg.Text    = avg > 0 ? avg.ToString("F1") : "—";
                    TxtAdminLogins.Text  = Fmt(row["LoginsToday"]);
                    TxtAdminActions.Text = Fmt(row["ActionsToday"]);
                }
                else if (SessionHelper.IsCurator)
                {
                    TxtCard1.Text = Fmt(row["StudentCount"]);
                    TxtCard2.Text = row["AttendancePercent"] != DBNull.Value
                                    ? row["AttendancePercent"] + "%" : "нет данных";
                    var avg = ToDecimal(row["AvgGrade"]);
                    TxtCard3.Text = avg > 0 ? avg.ToString("F1") : "—";
                    TxtCard4.Text = Fmt(row["AbsentCount"]);
                }
                else if (SessionHelper.IsHeadman)
                {
                    TxtCard1.Text = Fmt(row["StudentCount"]);
                    TxtCard2.Text = Fmt(row["AttendancePercent"]); // count repurposed
                    TxtCard3.Text = Fmt(row["AbsentCount"]);
                    TxtCard4.Text = Fmt(row["LessonsToday"]);
                }
                else // Student
                {
                    var avg = ToDecimal(row["AvgGrade"]);
                    TxtCard1.Text = avg > 0 ? avg.ToString("F1") : "—";
                    TxtCard2.Text = row["AttendancePercent"] != DBNull.Value
                                    ? row["AttendancePercent"] + "%" : "нет данных";
                    TxtCard3.Text = Fmt(row["AbsentCount"]);
                    TxtCard4.Text = Fmt(row["LessonsToday"]);
                }

                TxtGreeting.Text = BuildGreeting(groupName);
            }
            catch
            {
                /* карточки останутся "—" */
            }
        }

        private string BuildGreeting(string groupName)
        {
            if (SessionHelper.IsAdmin)   return "Системный дашборд";
            if (SessionHelper.IsCurator) return "Группа " + groupName;
            if (SessionHelper.IsHeadman) return "Группа " + groupName;
            return "Привет, " + SessionHelper.FirstName + "!"; // Student
        }

        // ────────────────────────────────────────────────────────────
        //  Лента событий
        // ────────────────────────────────────────────────────────────
        private void LoadEvents()
        {
            var events = new List<DashboardEvent>();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetDashboardEvents", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName),
                    new SqlParameter("@Limit",    10)
                });

                foreach (DataRow row in dt.Rows)
                {
                    var eventType = row["EventType"]?.ToString() ?? "";
                    int? logId = null;
                    if (dt.Columns.Contains("LogId") && row["LogId"] != DBNull.Value)
                        logId = Convert.ToInt32(row["LogId"]);

                    var timeVal = row["EventTime"];
                    string timeStr = timeVal != DBNull.Value
                        ? Convert.ToDateTime(timeVal).ToString("HH:mm dd.MM")
                        : "";

                    events.Add(new DashboardEvent
                    {
                        LogId    = logId,
                        Text     = row["EventText"]?.ToString() ?? "",
                        Time     = timeStr,
                        DotColor = DotColor(eventType),
                        CanOpen  = SessionHelper.IsAdmin && logId.HasValue
                    });
                }

                if (events.Count == 0)
                    events.Add(new DashboardEvent
                    {
                        Text = SessionHelper.IsAdmin
                            ? "Активность отсутствует"
                            : "Нет актуальных событий",
                        Time = "", DotColor = "#A19F9D"
                    });
            }
            catch
            {
                events.Add(new DashboardEvent
                {
                    Text     = "Добро пожаловать, " + SessionHelper.FullName,
                    Time     = DateTime.Now.ToString("HH:mm"),
                    DotColor = "#107C10"
                });
            }

            EventsList.ItemsSource = events;
        }

        // ────────────────────────────────────────────────────────────
        //  Расписание на сегодня (боковая панель)
        // ────────────────────────────────────────────────────────────
        private void LoadTodaySchedule()
        {
            try
            {
                // GroupId уже получен через sp_GetDashboard; передаём снова отдельным запросом
                int groupId = GetGroupId();
                if (groupId <= 0) { ShowNoLessons(); return; }

                var dt = DatabaseHelper.ExecuteProcedure("sp_GetTodaySchedule", new[]
                {
                    new SqlParameter("@GroupId", groupId)
                });

                if (dt == null || dt.Rows.Count == 0) { ShowNoLessons(); return; }

                var items = new List<ScheduleItem>();
                foreach (DataRow row in dt.Rows)
                {
                    items.Add(new ScheduleItem
                    {
                        LessonNumber = row["LessonNumber"]?.ToString() ?? "",
                        SubjectName  = row["SubjectName"]?.ToString()  ?? "",
                        Classroom    = row["Classroom"]?.ToString()    ?? "—",
                        StartTime    = row["StartTime"]?.ToString()    ?? "",
                        EndTime      = row["EndTime"]?.ToString()      ?? "",
                        TeacherShort = row["TeacherShort"]?.ToString() ?? ""
                    });
                }

                ScheduleList.ItemsSource = items;
                ScheduleList.Visibility  = Visibility.Visible;
                TxtNoLessons.Visibility  = Visibility.Collapsed;
            }
            catch
            {
                ShowNoLessons();
            }
        }

        private void ShowNoLessons()
        {
            ScheduleList.Visibility  = Visibility.Collapsed;
            TxtNoLessons.Visibility  = Visibility.Visible;
        }

        /// <summary>GroupId получен из sp_GetDashboard и закэширован в _groupId.</summary>
        private int GetGroupId() => _groupId;

        // ────────────────────────────────────────────────────────────
        //  Двойной клик — детали аудита (только Admin)
        // ────────────────────────────────────────────────────────────
        private void EventsList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(EventsList.SelectedItem is DashboardEvent ev)) return;
            if (!ev.CanOpen || !ev.LogId.HasValue) return;

            var win = new AuditDetailWindow(ev.LogId.Value);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        // ────────────────────────────────────────────────────────────
        //  Вспомогательные методы
        // ────────────────────────────────────────────────────────────
        private static string Fmt(object val) =>
            (val == null || val == DBNull.Value) ? "—" : val.ToString();

        private static decimal ToDecimal(object val)
        {
            if (val == null || val == DBNull.Value) return 0;
            return Convert.ToDecimal(val);
        }

        private static string DotColor(string eventType)
        {
            switch (eventType)
            {
                case "LOGIN":       return "#107C10";
                case "CREATE":      return "#0078D4";
                case "SOFT_DELETE": return "#D13438";
                case "UPDATE":      return "#CA5010";
                case "GRADE":       return "#0078D4";
                case "SCHEDULE":    return "#CA5010";
                case "ATTENDANCE":  return "#8764B8";
                default:            return "#A19F9D";
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Модели привязки данных
    // ────────────────────────────────────────────────────────────────
    public class DashboardEvent
    {
        public int?   LogId    { get; set; }
        public string Text     { get; set; }
        public string Time     { get; set; }
        public string DotColor { get; set; }
        public bool   CanOpen  { get; set; }
    }

    public class ScheduleItem
    {
        public string LessonNumber { get; set; }
        public string SubjectName  { get; set; }
        public string Classroom    { get; set; }
        public string StartTime    { get; set; }
        public string EndTime      { get; set; }
        public string TeacherShort { get; set; }
    }
}
