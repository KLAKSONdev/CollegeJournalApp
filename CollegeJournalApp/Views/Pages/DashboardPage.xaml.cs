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
        private int _groupId;

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadAll();
        }

        // ─────────────────────────────────────────────────────────────────
        private void LoadAll()
        {
            TxtDate.Text = DateTime.Now.ToString("dd MMMM yyyy", new CultureInfo("ru-RU"));
            SetupRoleLayout();
            LoadStats();
            LoadEvents();

            if (SessionHelper.IsAdmin)
            {
                LoadAdminGroupStats();
                LoadAdminAttendanceToday();
                LoadAdminAlerts();
            }
            else
            {
                LoadTodaySchedule();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Видимость секций по роли
        // ─────────────────────────────────────────────────────────────────
        private void SetupRoleLayout()
        {
            if (SessionHelper.IsAdmin)
            {
                TxtGreeting.Text           = "Системный дашборд";
                TxtSubtitle.Text           = "Администратор — полный обзор системы";
                AdminStatsPanel.Visibility = Visibility.Visible;
                AdminDashboardBody.Visibility = Visibility.Visible;
                NonAdminContent.Visibility = Visibility.Collapsed;
                TxtClickHint.Visibility    = Visibility.Visible;
            }
            else
            {
                AdminStatsPanel.Visibility    = Visibility.Collapsed;
                AdminDashboardBody.Visibility = Visibility.Collapsed;
                NonAdminContent.Visibility    = Visibility.Visible;

                if (SessionHelper.IsCurator)
                {
                    TxtGreeting.Text        = "Панель куратора";
                    TxtSubtitle.Text        = "Обзор вашей группы";
                    SchedulePanel.Visibility = Visibility.Visible;
                    TxtEventsTitle.Text     = "Последние оценки в группе";
                    SetCardLabels("Студентов в группе",  "#0078D4",
                                  "Посещаемость сегодня","#107C10",
                                  "Средний балл",         "#107C10",
                                  "Пропусков за 30 дней","#D13438");
                }
                else if (SessionHelper.IsHeadman)
                {
                    TxtGreeting.Text        = "Панель старосты";
                    TxtSubtitle.Text        = "Ваша группа сегодня";
                    SchedulePanel.Visibility = Visibility.Visible;
                    TxtEventsTitle.Text     = "Расписание на сегодня";
                    SetCardLabels("Студентов в группе",  "#0078D4",
                                  "Присутствует сегодня","#107C10",
                                  "Отсутствует сегодня", "#D13438",
                                  "Пар сегодня",         "#CA5010");
                }
                else // Student
                {
                    TxtGreeting.Text        = "Привет, " + SessionHelper.FirstName + "!";
                    TxtSubtitle.Text        = "Ваша успеваемость и расписание";
                    SchedulePanel.Visibility = Visibility.Visible;
                    TxtEventsTitle.Text     = "Мои последние оценки";
                    SetCardLabels("Мой средний балл",       "#107C10",
                                  "Посещаемость за месяц",  "#0078D4",
                                  "Пропусков за месяц",     "#D13438",
                                  "Пар сегодня",            "#CA5010");
                }

                TxtWeekday.Text = DateTime.Now.ToString("dddd", new CultureInfo("ru-RU"));
            }
        }

        private void SetCardLabels(
            string l1, string c1, string l2, string c2,
            string l3, string c3, string l4, string c4)
        {
            TxtLabel1.Text      = l1;
            TxtLabel2.Text      = l2;
            TxtLabel3.Text      = l3;
            TxtLabel4.Text      = l4;
            Card1Bar.Background = BrushOf(c1);
            Card2Bar.Background = BrushOf(c2);
            Card3Bar.Background = BrushOf(c3);
            Card4Bar.Background = BrushOf(c4);
        }

        // ─────────────────────────────────────────────────────────────────
        //  KPI-карточки
        // ─────────────────────────────────────────────────────────────────
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

                if (dt.Columns.Contains("GroupId") && row["GroupId"] != DBNull.Value)
                    _groupId = Convert.ToInt32(row["GroupId"]);

                var groupName = row["GroupName"]?.ToString() ?? "—";
                var mw = Application.Current.MainWindow as MainWindow;
                if (mw != null) mw.TxtGroupName.Text = groupName;

                if (SessionHelper.IsAdmin)
                {
                    TxtAdminUsers.Text    = Fmt(row["UserCount"]);
                    TxtAdminStudents.Text = Fmt(row["StudentCount"]);
                    TxtAdminGroups.Text   = Fmt(row["GroupCount"]);
                    TxtAdminTeachers.Text = Fmt(row["TeacherCount"]);
                    TxtAdminNewUsers.Text = Fmt(row["NewUsersThisMonth"]);

                    var avg = ToDouble(row["AvgGrade"]);
                    TxtAdminAvg.Text     = avg > 0 ? avg.ToString("F1") : "—";
                    TxtAdminLogins.Text  = Fmt(row["LoginsToday"]);
                    TxtAdminActions.Text = Fmt(row["ActionsToday"]);
                }
                else if (SessionHelper.IsCurator)
                {
                    TxtCard1.Text = Fmt(row["StudentCount"]);
                    TxtCard2.Text = row["AttendancePercent"] != DBNull.Value
                                    ? row["AttendancePercent"] + "%" : "—";
                    var avg = ToDouble(row["AvgGrade"]);
                    TxtCard3.Text = avg > 0 ? avg.ToString("F1") : "—";
                    TxtCard4.Text = Fmt(row["AbsentCount"]);
                    TxtGreeting.Text = "Группа " + groupName;
                }
                else if (SessionHelper.IsHeadman)
                {
                    TxtCard1.Text = Fmt(row["StudentCount"]);
                    TxtCard2.Text = Fmt(row["AttendancePercent"]);
                    TxtCard3.Text = Fmt(row["AbsentCount"]);
                    TxtCard4.Text = Fmt(row["LessonsToday"]);
                    TxtGreeting.Text = "Группа " + groupName;
                }
                else // Student
                {
                    var avg = ToDouble(row["AvgGrade"]);
                    TxtCard1.Text = avg > 0 ? avg.ToString("F1") : "—";
                    TxtCard2.Text = row["AttendancePercent"] != DBNull.Value
                                    ? row["AttendancePercent"] + "%" : "—";
                    TxtCard3.Text = Fmt(row["AbsentCount"]);
                    TxtCard4.Text = Fmt(row["LessonsToday"]);
                }
            }
            catch { /* карточки останутся "—" */ }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Лента событий
        // ─────────────────────────────────────────────────────────────────
        private void LoadEvents()
        {
            var events = new List<DashboardEvent>();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetDashboardEvents", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName),
                    new SqlParameter("@Limit",    15)
                });

                foreach (DataRow row in dt.Rows)
                {
                    var eventType = row["EventType"]?.ToString() ?? "";
                    int? logId = dt.Columns.Contains("LogId") && row["LogId"] != DBNull.Value
                                 ? Convert.ToInt32(row["LogId"]) : (int?)null;

                    var timeVal = row["EventTime"];
                    string timeStr = timeVal != DBNull.Value
                        ? Convert.ToDateTime(timeVal).ToString("HH:mm dd.MM")
                        : "";

                    events.Add(new DashboardEvent
                    {
                        LogId    = logId,
                        Text     = row["EventText"]?.ToString() ?? "",
                        Time     = timeStr,
                        DotColor = EventDotColor(eventType),
                        CanOpen  = SessionHelper.IsAdmin && logId.HasValue
                    });
                }

                if (events.Count == 0)
                    events.Add(new DashboardEvent
                    {
                        Text     = SessionHelper.IsAdmin ? "Активность отсутствует" : "Нет актуальных событий",
                        Time     = "",
                        DotColor = "#A19F9D"
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

            if (SessionHelper.IsAdmin)
                EventsList.ItemsSource = events;
            else
                NonAdminEventsList.ItemsSource = events;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Admin: Успеваемость по группам
        // ─────────────────────────────────────────────────────────────────
        private void LoadAdminGroupStats()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAdminGroupStats");
                var items = new List<GroupStatItem>();

                foreach (DataRow r in dt.Rows)
                {
                    double avg    = r["AvgGrade"]      != DBNull.Value ? Convert.ToDouble(r["AvgGrade"])    : 0;
                    int    fail   = r["FailCount"]     != DBNull.Value ? Convert.ToInt32(r["FailCount"])    : 0;
                    double excPct = r["ExcellentPct"]  != DBNull.Value ? Convert.ToDouble(r["ExcellentPct"]) : 0;
                    int    course = r["Course"]        != DBNull.Value ? Convert.ToInt32(r["Course"])        : 0;
                    int    stCnt  = r["StudentCount"]  != DBNull.Value ? Convert.ToInt32(r["StudentCount"]) : 0;

                    SolidColorBrush avgColor;
                    if      (avg >= 4.5) avgColor = BrushOf("#107C10");
                    else if (avg >= 3.5) avgColor = BrushOf("#0078D4");
                    else if (avg > 0)    avgColor = BrushOf("#CA5010");
                    else                 avgColor = BrushOf("#A19F9D");

                    items.Add(new GroupStatItem
                    {
                        GroupName      = r["GroupName"]?.ToString() ?? "",
                        CourseLine     = course > 0 ? course + " курс" : "",
                        StudentCount   = stCnt,
                        AvgGradeStr    = avg > 0 ? avg.ToString("F1") : "—",
                        AvgColor       = avgColor,
                        FailCount      = fail,
                        ExcellentPctStr= excPct > 0 ? excPct.ToString("F0") + "%" : "0%"
                    });
                }

                if (items.Count > 0)
                {
                    GroupStatsList.ItemsSource   = items;
                    GroupStatsList.Visibility    = Visibility.Visible;
                    TxtNoGroupStats.Visibility   = Visibility.Collapsed;
                }
                else
                {
                    GroupStatsList.Visibility    = Visibility.Collapsed;
                    TxtNoGroupStats.Visibility   = Visibility.Visible;
                }
            }
            catch
            {
                TxtNoGroupStats.Visibility = Visibility.Visible;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Admin: Посещаемость сегодня
        // ─────────────────────────────────────────────────────────────────
        private void LoadAdminAttendanceToday()
        {
            TxtTodayDate.Text = DateTime.Today.ToString("d MMMM", new CultureInfo("ru-RU"));

            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAdminAttendanceToday");
                var items = new List<AttendanceGroupItem>();

                foreach (DataRow r in dt.Rows)
                {
                    int total   = r["TotalStudents"] != DBNull.Value ? Convert.ToInt32(r["TotalStudents"]) : 0;
                    int present = r["PresentCount"]  != DBNull.Value ? Convert.ToInt32(r["PresentCount"])  : 0;
                    int absent  = r["AbsentCount"]   != DBNull.Value ? Convert.ToInt32(r["AbsentCount"])   : 0;
                    int late    = r["LateCount"]     != DBNull.Value ? Convert.ToInt32(r["LateCount"])     : 0;
                    int marked  = present + absent + late;

                    double pct = total > 0 ? (double)present / total * 100.0 : 0;
                    bool hasData = marked > 0;

                    string pctColor, barColor;
                    if (!hasData) { pctColor = "#A19F9D"; barColor = "#D0D0D0"; }
                    else if (pct >= 80) { pctColor = "#107C10"; barColor = "#107C10"; }
                    else if (pct >= 60) { pctColor = "#CA5010"; barColor = "#CA5010"; }
                    else               { pctColor = "#D13438"; barColor = "#D13438"; }

                    var detail = new List<string>();
                    if (present > 0) detail.Add("Присут.: " + present);
                    if (absent  > 0) detail.Add("Отсутст.: " + absent);
                    if (late    > 0) detail.Add("Опозд.: " + late);

                    items.Add(new AttendanceGroupItem
                    {
                        GroupName  = r["GroupName"]?.ToString() ?? "",
                        Summary    = hasData ? present + "/" + total + " (" + pct.ToString("F0") + "%)" : "не отмечено",
                        PctColor   = BrushOf(pctColor),
                        BarWidth   = total > 0 ? (double)present / total * 200.0 : 0,
                        BarColor   = BrushOf(barColor),
                        DetailLine = detail.Count > 0 ? string.Join(" · ", detail) : "нет данных"
                    });
                }

                // Отделяем группы с данными — показываем первыми
                items.Sort((a, b) =>
                {
                    bool aHas = a.Summary != "не отмечено";
                    bool bHas = b.Summary != "не отмечено";
                    if (aHas != bHas) return bHas.CompareTo(aHas); // группы с данными первые
                    return string.Compare(a.GroupName, b.GroupName, StringComparison.CurrentCulture);
                });

                if (items.Count > 0)
                {
                    AttendanceList.ItemsSource  = items;
                    AttendanceList.Visibility   = Visibility.Visible;
                    TxtNoAttendance.Visibility  = Visibility.Collapsed;
                }
                else
                {
                    AttendanceList.Visibility   = Visibility.Collapsed;
                    TxtNoAttendance.Visibility  = Visibility.Visible;
                }
            }
            catch
            {
                TxtNoAttendance.Visibility = Visibility.Visible;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Admin: Тревоги о проблемных студентах
        // ─────────────────────────────────────────────────────────────────
        private void LoadAdminAlerts()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAdminAlerts");
                var items = new List<AlertItem>();

                foreach (DataRow r in dt.Rows)
                {
                    int twos = r["TwosCount"]    != DBNull.Value ? Convert.ToInt32(r["TwosCount"])    : 0;
                    int abs  = r["AbsencesCount"] != DBNull.Value ? Convert.ToInt32(r["AbsencesCount"]) : 0;

                    // Критично — и двойки, и пропуски
                    bool critical = twos >= 2 && abs >= 4;

                    var details = new List<string>();
                    if (twos > 0) details.Add(twos + " " + Plural(twos, "двойка", "двойки", "двоек") + " за месяц");
                    if (abs  > 0) details.Add(abs  + " " + Plural(abs,  "пропуск","пропуска","пропусков") + " за 30 дн.");

                    items.Add(new AlertItem
                    {
                        StudentName  = r["StudentName"]?.ToString() ?? "",
                        GroupName    = r["GroupName"]?.ToString()   ?? "",
                        AlertDetails = string.Join(" · ", details),
                        AlertColor   = BrushOf(critical ? "#D13438" : "#CA5010")
                    });
                }

                if (items.Count > 0)
                {
                    AlertsList.ItemsSource  = items;
                    AlertsList.Visibility   = Visibility.Visible;
                    TxtNoAlerts.Visibility  = Visibility.Collapsed;
                }
                else
                {
                    AlertsList.Visibility   = Visibility.Collapsed;
                    TxtNoAlerts.Visibility  = Visibility.Visible;
                }
            }
            catch
            {
                TxtNoAlerts.Visibility = Visibility.Visible;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Расписание на сегодня (не-Admin)
        // ─────────────────────────────────────────────────────────────────
        private void LoadTodaySchedule()
        {
            try
            {
                int groupId = _groupId;
                if (groupId <= 0) { ShowNoLessons(); return; }

                var dt = DatabaseHelper.ExecuteProcedure("sp_GetTodaySchedule", new[]
                {
                    new SqlParameter("@GroupId", groupId)
                });
                if (dt == null || dt.Rows.Count == 0) { ShowNoLessons(); return; }

                var items = new List<ScheduleItem>();
                foreach (DataRow row in dt.Rows)
                    items.Add(new ScheduleItem
                    {
                        LessonNumber = row["LessonNumber"]?.ToString() ?? "",
                        SubjectName  = row["SubjectName"]?.ToString()  ?? "",
                        Classroom    = row["Classroom"]?.ToString()    ?? "—",
                        StartTime    = row["StartTime"]?.ToString()    ?? "",
                        EndTime      = row["EndTime"]?.ToString()      ?? "",
                        TeacherShort = row["TeacherShort"]?.ToString() ?? ""
                    });

                ScheduleList.ItemsSource  = items;
                ScheduleList.Visibility   = Visibility.Visible;
                TxtNoLessons.Visibility   = Visibility.Collapsed;
            }
            catch { ShowNoLessons(); }
        }

        private void ShowNoLessons()
        {
            ScheduleList.Visibility  = Visibility.Collapsed;
            TxtNoLessons.Visibility  = Visibility.Visible;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Двойной клик по аудит-строке (Admin)
        // ─────────────────────────────────────────────────────────────────
        private void EventsList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(EventsList.SelectedItem is DashboardEvent ev)) return;
            if (!ev.CanOpen || !ev.LogId.HasValue) return;
            var win = new AuditDetailWindow(ev.LogId.Value);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Утилиты
        // ─────────────────────────────────────────────────────────────────
        private static string Fmt(object val) =>
            (val == null || val == DBNull.Value) ? "—" : val.ToString();

        private static double ToDouble(object val)
        {
            if (val == null || val == DBNull.Value) return 0;
            return Convert.ToDouble(val);
        }

        private static SolidColorBrush BrushOf(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        private static string EventDotColor(string eventType)
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

        /// <summary>Русское склонение числительных.</summary>
        private static string Plural(int n, string one, string few, string many)
        {
            int abs = Math.Abs(n);
            int mod10 = abs % 10, mod100 = abs % 100;
            if (mod10 == 1 && mod100 != 11) return one;
            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return few;
            return many;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Модели привязки данных
    // ─────────────────────────────────────────────────────────────────────

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

    public class GroupStatItem
    {
        public string          GroupName       { get; set; }
        public string          CourseLine      { get; set; }
        public int             StudentCount    { get; set; }
        public string          AvgGradeStr     { get; set; }
        public SolidColorBrush AvgColor        { get; set; }
        public int             FailCount       { get; set; }
        public string          ExcellentPctStr { get; set; }
    }

    public class AttendanceGroupItem
    {
        public string          GroupName  { get; set; }
        public string          Summary    { get; set; }
        public SolidColorBrush PctColor   { get; set; }
        public double          BarWidth   { get; set; }
        public SolidColorBrush BarColor   { get; set; }
        public string          DetailLine { get; set; }
    }

    public class AlertItem
    {
        public string          StudentName  { get; set; }
        public string          GroupName    { get; set; }
        public string          AlertDetails { get; set; }
        public SolidColorBrush AlertColor   { get; set; }
    }
}
