using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class StudentsPage : Page
    {
        private List<StudentRow> _allStudents = new List<StudentRow>();
        private bool _loaded = false;

        public StudentsPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadStudents();
        }

        // ═══════════════════════════════════════════
        // ЗАГРУЗКА СПИСКА СТУДЕНТОВ
        // ═══════════════════════════════════════════
        private void LoadStudents()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentsByRole", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });

                _allStudents.Clear();
                int i = 1;
                foreach (DataRow row in dt.Rows)
                {
                    var dorm = row["DormitoryName"] != DBNull.Value
                        ? row["DormitoryName"].ToString() +
                          (row["RoomNumber"] != DBNull.Value ? ", к." + row["RoomNumber"] : "")
                        : "—";

                    bool isHead = row["IsHeadman"] != DBNull.Value && Convert.ToBoolean(row["IsHeadman"]);

                    _allStudents.Add(new StudentRow
                    {
                        RowNum      = i++,
                        StudentId   = Convert.ToInt32(row["StudentId"]),
                        FullName    = row["FullName"]?.ToString(),
                        StudentCode = row["StudentCode"]?.ToString() ?? "—",
                        BirthDate   = row["BirthDate"] != DBNull.Value
                                      ? Convert.ToDateTime(row["BirthDate"]).ToString("dd.MM.yyyy") : "—",
                        Gender      = row["Gender"]?.ToString() ?? "—",
                        StudyBasis  = row["StudyBasis"]?.ToString() ?? "—",
                        Dormitory   = dorm,
                        Phone       = row["Phone"]?.ToString() ?? "—",
                        IsHeadman   = isHead,
                        Status      = isHead ? "Староста" : "Студент",
                        StatusColor = isHead ? "#0078D4" : "#605E5C"
                    });
                }

                TxtStudentCount.Text = $"— {_allStudents.Count} чел.";
                _loaded = true;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки студентов:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyFilters()
        {
            if (!_loaded) return;

            var filtered = _allStudents.AsEnumerable();

            var search = TxtSearch?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(s => s.FullName != null &&
                                               s.FullName.ToLower().Contains(search));

            StudentsList.ItemsSource = filtered.ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        // ═══════════════════════════════════════════
        // ВЫБОР СТУДЕНТА — ЗАГРУЗКА КАРТОЧКИ
        // ═══════════════════════════════════════════
        private void StudentsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StudentsList.SelectedItem is StudentRow row)
                LoadStudentCard(row.StudentId, row);
        }

        private void LoadStudentCard(int studentId, StudentRow row)
        {
            PanelEmpty.Visibility = Visibility.Collapsed;
            PanelCard.Visibility  = Visibility.Visible;

            // Шапка
            TxtAvatar.Text          = row.FullName?.Substring(0, 1).ToUpper() ?? "?";
            TxtCardName.Text        = row.FullName;
            TxtCardStatus.Text      = row.Status;
            TxtCardCode.Text        = "Группа: ИС-23-1";
            TxtCardStudentCode.Text = row.StudentCode;
            BdrStatus.Background    = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(row.StatusColor));

            // Загружаем все данные
            LoadPersonalData(studentId, row);
            LoadSocialData(studentId);
            LoadParents(studentId);
            LoadGrades(studentId);
            LoadAttendance(studentId);
            LoadAchievements(studentId);
        }

        // ── Личные данные ──
        private void LoadPersonalData(int studentId, StudentRow row)
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentDetails",
                    new[] { new SqlParameter("@StudentId", studentId) });

                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                TxtBirthDate.Text    = r["BirthDate"]         != DBNull.Value ? Convert.ToDateTime(r["BirthDate"]).ToString("dd.MM.yyyy") : "—";
                TxtGender.Text       = r["Gender"]?.ToString() ?? "—";
                TxtBirthPlace.Text   = r["BirthPlace"]?.ToString() ?? "—";
                TxtCitizenship.Text  = r["Citizenship"]?.ToString() ?? "—";
                TxtAddress.Text      = r["Address"]?.ToString() ?? "—";
                TxtPhone.Text        = r["Phone"]?.ToString() ?? "—";
                TxtEmail.Text        = r["Email"]?.ToString() ?? "—";
                TxtDorm.Text         = row.Dormitory;
                TxtStudyBasis.Text   = r["StudyBasis"]?.ToString() ?? "—";

                // Паспорт
                var series = r["PassportSeries"]?.ToString() ?? "";
                var number = r["PassportNumber"]?.ToString() ?? "";
                TxtPassport.Text     = (series + " " + number).Trim().Length > 0 ? series + " " + number : "—";
                TxtPassportDate.Text = r["PassportIssuedDate"] != DBNull.Value ? Convert.ToDateTime(r["PassportIssuedDate"]).ToString("dd.MM.yyyy") : "—";
                TxtPassportBy.Text   = r["PassportIssuedBy"]?.ToString() ?? "—";
                TxtSnils.Text        = r["SNILSNumber"]?.ToString() ?? "—";

                var prev = r["PreviousSchool"]?.ToString() ?? "";
                var prevType = r["PreviousSchoolType"]?.ToString() ?? "";
                TxtPrevSchool.Text   = string.IsNullOrEmpty(prev) ? "—" : $"{prev} ({prevType})";
            }
            catch { }
        }

        // ── Социальная карточка ──
        private void LoadSocialData(int studentId)
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentSocial",
                    new[] { new SqlParameter("@StudentId", studentId) });

                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                TxtHealthGroup.Text    = r["HealthGroup"]?.ToString() ?? "—";
                TxtDisability.Text     = r["DisabilityGroup"] != DBNull.Value
                    ? $"{r["Disability"]} (гр. {r["DisabilityGroup"]})" : "Нет";
                TxtChronic.Text        = r["ChronicDiseases"]?.ToString() ?? "—";
                TxtFamilyStructure.Text= r["FamilyStructure"]?.ToString() ?? "—";
                TxtHousing.Text        = r["HousingCondition"]?.ToString() ?? "—";
                TxtSocialNotes.Text    = r["AdditionalNotes"]?.ToString() ?? "—";

                // Статусы
                var statuses = new List<string>();
                if (r["IsOrphan"]           != DBNull.Value && Convert.ToBoolean(r["IsOrphan"]))           statuses.Add("Сирота");
                if (r["IsHalfOrphan"]       != DBNull.Value && Convert.ToBoolean(r["IsHalfOrphan"]))       statuses.Add("Полусирота");
                if (r["IsFromLargeFamily"]  != DBNull.Value && Convert.ToBoolean(r["IsFromLargeFamily"]))  statuses.Add("Многодетная семья");
                if (r["IsLowIncome"]        != DBNull.Value && Convert.ToBoolean(r["IsLowIncome"]))        statuses.Add("Малоимущий");
                if (r["IsSociallyVulnerable"]!=DBNull.Value && Convert.ToBoolean(r["IsSociallyVulnerable"])) statuses.Add("Соц. незащищённый");
                if (r["IsOnGuardianship"]   != DBNull.Value && Convert.ToBoolean(r["IsOnGuardianship"]))   statuses.Add("Опека/попечительство");

                SocialStatusList.ItemsSource = statuses.Count > 0 ? statuses : new List<string> { "Нет особых статусов" };
            }
            catch { }
        }

        // ── Родители ──
        private void LoadParents(int studentId)
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentParents",
                    new[] { new SqlParameter("@StudentId", studentId) });

                var parents = new List<ParentRow>();
                foreach (DataRow r in dt.Rows)
                {
                    parents.Add(new ParentRow
                    {
                        Relation  = r["Relation"]?.ToString() ?? "—",
                        FullName  = r["LastName"] + " " + r["FirstName"] + " " + r["MiddleName"],
                        Phone     = r["Phone"]?.ToString() ?? "—",
                        WorkPhone = r["WorkPhone"]?.ToString() ?? "—",
                        Workplace = r["Workplace"]?.ToString() ?? "—",
                        Position  = r["Position"]?.ToString() ?? "—",
                        Education = r["Education"]?.ToString() ?? "—"
                    });
                }

                ParentsList.ItemsSource = parents.Count > 0 ? parents : null;
            }
            catch { }
        }

        // ── Оценки ──
        private void LoadGrades(int studentId)
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentGrades",
                    new[] { new SqlParameter("@StudentId", studentId) });

                var grades = new List<GradeRow>();
                foreach (DataRow r in dt.Rows)
                {
                    grades.Add(new GradeRow
                    {
                        SubjectName = r["SubjectName"]?.ToString() ?? "—",
                        GradeType   = r["GradeType"]?.ToString()   ?? "—",
                        GradeValue  = r["GradeValue"]?.ToString()   ?? "—",
                        GradeDate   = r["GradeDate"] != DBNull.Value ? Convert.ToDateTime(r["GradeDate"]).ToString("dd.MM.yyyy") : "—"
                    });
                }
                GradesGrid.ItemsSource = grades;
            }
            catch { }
        }

        // ── Посещаемость ──
        private void LoadAttendance(int studentId)
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentAttendance",
                    new[] { new SqlParameter("@StudentId", studentId) });

                int present = 0, absent = 0, late = 0, excused = 0;
                var rows = new List<AttRow>();

                foreach (DataRow r in dt.Rows)
                {
                    var status = r["Status"]?.ToString() ?? "";
                    if (status == "Присутствовал")       present++;
                    else if (status == "Отсутствовал")   absent++;
                    else if (status == "Опоздал")        late++;
                    else if (status == "Уважительная причина") excused++;

                    rows.Add(new AttRow
                    {
                        LessonDate = r["LessonDate"] != DBNull.Value ? Convert.ToDateTime(r["LessonDate"]).ToString("dd.MM.yyyy") : "—",
                        Subject    = r["SubjectName"]?.ToString() ?? "—",
                        Status     = status,
                        Reason     = r["Reason"]?.ToString() ?? "—"
                    });
                }

                int total = present + absent + late + excused;
                TxtPresent.Text    = present.ToString();
                TxtAbsent.Text     = absent.ToString();
                TxtLate.Text       = late.ToString();
                TxtAttPercent.Text = total > 0 ? $"{Math.Round(100.0 * present / total, 1)}%" : "—";

                AttendanceGrid.ItemsSource = rows;
            }
            catch { }
        }

        // ── Достижения ──
        private void LoadAchievements(int studentId)
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentAchievements",
                    new[] { new SqlParameter("@StudentId", studentId) });

                var list = new List<AchRow>();
                foreach (DataRow r in dt.Rows)
                {
                    list.Add(new AchRow
                    {
                        Title       = r["Title"]?.ToString()       ?? "—",
                        Category    = r["Category"]?.ToString()    ?? "—",
                        Level       = r["Level"]?.ToString()       ?? "—",
                        Description = r["Description"]?.ToString() ?? "",
                        AchieveDate = r["AchieveDate"] != DBNull.Value ? Convert.ToDateTime(r["AchieveDate"]).ToString("dd.MM.yyyy") : "—"
                    });
                }
                AchievementsList.ItemsSource = list;
            }
            catch { }
        }
    }

    // ─── Модели ───
    public class StudentRow
    {
        public int    RowNum      { get; set; }
        public int    StudentId   { get; set; }
        public string FullName    { get; set; }
        public string StudentCode { get; set; }
        public string BirthDate   { get; set; }
        public string Gender      { get; set; }
        public string StudyBasis  { get; set; }
        public string Dormitory   { get; set; }
        public string Phone       { get; set; }
        public bool   IsHeadman   { get; set; }
        public string Status      { get; set; }
        public string StatusColor { get; set; }
    }

    public class ParentRow
    {
        public string Relation  { get; set; }
        public string FullName  { get; set; }
        public string Phone     { get; set; }
        public string WorkPhone { get; set; }
        public string Workplace { get; set; }
        public string Position  { get; set; }
        public string Education { get; set; }
    }

    public class GradeRow
    {
        public string SubjectName { get; set; }
        public string GradeType   { get; set; }
        public string GradeValue  { get; set; }
        public string GradeDate   { get; set; }
    }

    public class AttRow
    {
        public string LessonDate { get; set; }
        public string Subject    { get; set; }
        public string Status     { get; set; }
        public string Reason     { get; set; }
    }

    public class AchRow
    {
        public string Title       { get; set; }
        public string Category    { get; set; }
        public string Level       { get; set; }
        public string Description { get; set; }
        public string AchieveDate { get; set; }
    }
}
