using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CollegeJournalApp.Views
{
    public partial class StudentCardWindow : Window
    {
        private readonly int _studentId;
        private readonly string _studentName;
        private bool _canEditPhoto;

        public StudentCardWindow(int studentId, string studentName)
        {
            InitializeComponent();
            _studentId   = studentId;
            _studentName = studentName;
            Title = $"Карточка — {studentName}";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _canEditPhoto = SessionHelper.IsCurator || SessionHelper.IsAdmin;
            ApplyRoleVisibility();
            LoadAllData();
        }

        private void ApplyRoleVisibility()
        {
            bool isCuratorOrAdmin = SessionHelper.IsCurator || SessionHelper.IsAdmin;
            bool isHeadmanOrAbove = SessionHelper.IsHeadman || isCuratorOrAdmin;

            TabPersonal.Visibility     = isHeadmanOrAbove ? Visibility.Visible : Visibility.Collapsed;
            PanelPassport.Visibility   = isCuratorOrAdmin ? Visibility.Visible : Visibility.Collapsed;
            TabSocial.Visibility       = isCuratorOrAdmin ? Visibility.Visible : Visibility.Collapsed;
            TabParents.Visibility      = isCuratorOrAdmin ? Visibility.Visible : Visibility.Collapsed;
            TabDocuments.Visibility    = isCuratorOrAdmin ? Visibility.Visible : Visibility.Collapsed;
            TabGrades.Visibility       = isHeadmanOrAbove ? Visibility.Visible : Visibility.Collapsed;
            TabAttendance.Visibility   = isHeadmanOrAbove ? Visibility.Visible : Visibility.Collapsed;
            TabAchievements.Visibility = isHeadmanOrAbove ? Visibility.Visible : Visibility.Collapsed;

            // Кнопки фото — только куратор и админ
            BtnUploadPhoto.Visibility  = _canEditPhoto ? Visibility.Visible : Visibility.Collapsed;
            BtnDeletePhoto.Visibility  = _canEditPhoto ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadAllData()
        {
            bool isCuratorOrAdmin = SessionHelper.IsCurator || SessionHelper.IsAdmin;
            bool isHeadmanOrAbove = SessionHelper.IsHeadman || isCuratorOrAdmin;

            LoadPersonalData();
            if (isCuratorOrAdmin)  { LoadSocialData(); LoadParents(); LoadDocuments(); }
            if (isHeadmanOrAbove)  { LoadGrades();     LoadAttendance(); LoadAchievements(); }
        }

        private void LoadPersonalData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentDetails",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                if (dt == null || dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                // Фото
                LoadPhoto(r);

                // Шапка
                bool isHead = r["IsHeadman"] != DBNull.Value && Convert.ToBoolean(r["IsHeadman"]);
                TxtName.Text   = _studentName;
                TxtStatus.Text = isHead ? "Староста" : "Студент";
                TxtGroup.Text  = r["GroupName"]?.ToString() ?? "—";
                TxtCode.Text   = r["StudentCode"]?.ToString() ?? "—";
                if (!isHead)
                    BdrStatus.Background = new SolidColorBrush(Color.FromRgb(96, 94, 92));

                // Личные данные
                TxtBirthDate.Text   = r["BirthDate"] != DBNull.Value ? Convert.ToDateTime(r["BirthDate"]).ToString("dd.MM.yyyy") : "—";
                TxtGender.Text      = r["Gender"]?.ToString()      ?? "—";
                TxtBirthPlace.Text  = r["BirthPlace"]?.ToString()  ?? "—";
                TxtCitizenship.Text = r["Citizenship"]?.ToString() ?? "—";
                TxtAddress.Text     = r["Address"]?.ToString()     ?? "—";
                TxtPhone.Text       = r["Phone"]?.ToString()       ?? "—";
                TxtEmail.Text       = r["Email"]?.ToString()       ?? "—";
                TxtBasis.Text       = r["StudyBasis"]?.ToString()  ?? "—";

                var dormName = r["DormitoryName"] != DBNull.Value ? r["DormitoryName"].ToString() : "";
                var roomNum  = r["RoomNumber"]    != DBNull.Value ? ", к." + r["RoomNumber"]      : "";
                TxtDorm.Text = string.IsNullOrEmpty(dormName) ? "Не проживает" : dormName + roomNum;

                // Паспорт
                var series = r["PassportSeries"]?.ToString() ?? "";
                var number = r["PassportNumber"]?.ToString() ?? "";
                TxtPassport.Text     = series.Length > 0 ? $"{series} {number}" : "—";
                TxtPassportDate.Text = r["PassportIssuedDate"] != DBNull.Value ? Convert.ToDateTime(r["PassportIssuedDate"]).ToString("dd.MM.yyyy") : "—";
                TxtPassportBy.Text   = r["PassportIssuedBy"]?.ToString() ?? "—";
                TxtSnils.Text        = r["SNILSNumber"]?.ToString() ?? "—";
                var prev     = r["PreviousSchool"]?.ToString()     ?? "";
                var prevType = r["PreviousSchoolType"]?.ToString() ?? "";
                TxtPrevSchool.Text = string.IsNullOrEmpty(prev) ? "—" : $"{prev} ({prevType})";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки личных данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── Фото ──────────────────────────────────────────────
        private void LoadPhoto(DataRow r)
        {
            try
            {
                if (r["PhotoData"] != DBNull.Value && r["PhotoData"] != null)
                {
                    var bytes = (byte[])r["PhotoData"];
                    SetPhotoFromBytes(bytes);
                    BtnDeletePhoto.IsEnabled = true;
                }
                else
                {
                    // Аватар-заглушка с инициалами
                    ImgPhoto.Source = null;
                    TxtAvatar.Visibility = Visibility.Visible;
                    TxtAvatar.Text = _studentName?.Length > 0 ? _studentName.Substring(0, 1).ToUpper() : "?";

                    BtnDeletePhoto.IsEnabled = false;
                }
            }
            catch { }
        }

        private void SetPhotoFromBytes(byte[] bytes)
        {
            var bmp = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                bmp.BeginInit();
                bmp.StreamSource     = ms;
                bmp.CacheOption      = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 160;
                bmp.EndInit();
                bmp.Freeze();
            }
            ImgPhoto.Source      = bmp;
            TxtAvatar.Visibility = Visibility.Collapsed;

        }

        private void BtnUploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Выберите фото студента",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var fileInfo = new FileInfo(dlg.FileName);
                if (fileInfo.Length > 5 * 1024 * 1024)
                {
                    MessageBox.Show("Файл слишком большой. Максимальный размер — 5 МБ.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var bytes    = File.ReadAllBytes(dlg.FileName);
                var ext      = Path.GetExtension(dlg.FileName).ToLower();
                var mimeType = ext == ".png" ? "image/png" : "image/jpeg";

                DatabaseHelper.ExecuteNonQuery("sp_UploadStudentPhoto", new[]
                {
                    new SqlParameter("@StudentId",    _studentId),
                    new SqlParameter("@PhotoData",    bytes),
                    new SqlParameter("@MimeType",     mimeType),
                    new SqlParameter("@UploadedById", SessionHelper.UserId)
                });

                SetPhotoFromBytes(bytes);
                BtnDeletePhoto.IsEnabled = true;
                MessageBox.Show("Фото успешно загружено!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки фото:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeletePhoto_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Удалить фото студента?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_DeleteStudentPhoto", new[]
                {
                    new SqlParameter("@StudentId",   _studentId),
                    new SqlParameter("@DeletedById", SessionHelper.UserId)
                });

                ImgPhoto.Source            = null;
                TxtAvatar.Visibility       = Visibility.Visible;
                TxtAvatar.Text             = _studentName?.Length > 0 ? _studentName.Substring(0, 1).ToUpper() : "?";

                BtnDeletePhoto.IsEnabled   = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления фото:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Остальные вкладки ──────────────────────────────────
        private void LoadSocialData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentSocial",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                if (dt == null || dt.Rows.Count == 0)
                { SocialStatusList.ItemsSource = new List<string> { "Нет данных" }; return; }
                var r = dt.Rows[0];

                TxtHealthGroup.Text     = NullOrDash(r["HealthGroup"]);
                TxtDisability.Text      = r["DisabilityGroup"] != DBNull.Value
                    ? $"{r["Disability"]} (гр. {r["DisabilityGroup"]})" : "Нет";
                TxtChronic.Text         = NullOrDash(r["ChronicDiseases"]);
                TxtFamilyStructure.Text = NullOrDash(r["FamilyStructure"]);
                TxtHousing.Text         = NullOrDash(r["HousingCondition"]);
                TxtSocialNotes.Text     = NullOrDash(r["AdditionalNotes"]);

                var statuses = new List<string>();
                if (ToBool(r["IsOrphan"]))             statuses.Add("Сирота");
                if (ToBool(r["IsHalfOrphan"]))         statuses.Add("Полусирота");
                if (ToBool(r["IsFromLargeFamily"]))    statuses.Add("Многодетная семья");
                if (ToBool(r["IsLowIncome"]))          statuses.Add("Малоимущий");
                if (ToBool(r["IsSociallyVulnerable"])) statuses.Add("Соц. незащищённый");
                if (ToBool(r["IsOnGuardianship"]))     statuses.Add("Опека/попечительство");
                SocialStatusList.ItemsSource = statuses.Count > 0 ? statuses : new List<string> { "Нет особых статусов" };
            }
            catch { SocialStatusList.ItemsSource = new List<string> { "Нет данных" }; }
        }

        private void LoadParents()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentParents",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                var list = new List<ParentRow>();
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        list.Add(new ParentRow
                        {
                            Relation  = r["Relation"]?.ToString()  ?? "—",
                            FullName  = $"{r["LastName"]} {r["FirstName"]} {r["MiddleName"]}".Trim(),
                            Phone     = NullOrDash(r["Phone"]),
                            WorkPhone = NullOrDash(r["WorkPhone"]),
                            Workplace = NullOrDash(r["Workplace"]),
                            Position  = NullOrDash(r["Position"]),
                            Education = NullOrDash(r["Education"])
                        });
                ParentsList.ItemsSource = list;
            }
            catch { }
        }

        private void LoadDocuments()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentDocuments", new[]
                {
                    new SqlParameter("@StudentId",    _studentId),
                    new SqlParameter("@ViewerUserId", SessionHelper.UserId)
                });
                var list = new List<DocumentRow>();
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        list.Add(new DocumentRow
                        {
                            Title        = r["Title"]?.ToString()        ?? "—",
                            DocumentType = r["DocumentType"]?.ToString() ?? "—",
                            FileSize     = NullOrDash(r["FileSize"]),
                            UploadedAt   = r["UploadedAt"] != DBNull.Value ? Convert.ToDateTime(r["UploadedAt"]).ToString("dd.MM.yyyy") : "—",
                            UploadedBy   = NullOrDash(r["UploadedBy"]),
                            Description  = r["Description"]?.ToString() ?? ""
                        });
                DocumentsGrid.ItemsSource = list;
                TxtNoDocuments.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void LoadGrades()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentGrades",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                var list = new List<GradeRow>();
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        list.Add(new GradeRow
                        {
                            SubjectName = r["SubjectName"]?.ToString() ?? "—",
                            GradeType   = r["GradeType"]?.ToString()   ?? "—",
                            GradeValue  = r["GradeValue"]?.ToString()  ?? "—",
                            GradeDate   = r["GradeDate"] != DBNull.Value ? Convert.ToDateTime(r["GradeDate"]).ToString("dd.MM.yyyy") : "—"
                        });
                GradesGrid.ItemsSource = list;
            }
            catch { }
        }

        private void LoadAttendance()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentAttendance",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                int present = 0, absent = 0, late = 0;
                var rows = new List<AttRow>();
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                    {
                        var st = r["Status"]?.ToString() ?? "";
                        if (st == "Присутствовал")     present++;
                        else if (st == "Отсутствовал") absent++;
                        else if (st == "Опоздал")      late++;
                        rows.Add(new AttRow
                        {
                            LessonDate = r["LessonDate"] != DBNull.Value ? Convert.ToDateTime(r["LessonDate"]).ToString("dd.MM.yyyy") : "—",
                            Subject    = r["SubjectName"]?.ToString() ?? "—",
                            Status     = st,
                            Reason     = NullOrDash(r["Reason"])
                        });
                    }
                int total = present + absent + late;
                TxtPresent.Text = present.ToString();
                TxtAbsent.Text  = absent.ToString();
                TxtLate.Text    = late.ToString();
                TxtPercent.Text = total > 0 ? $"{Math.Round(100.0 * present / total, 1)}%" : "—";
                AttGrid.ItemsSource = rows;
            }
            catch { }
        }

        private void LoadAchievements()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentAchievements",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                var list = new List<AchRow>();
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        list.Add(new AchRow
                        {
                            Title       = r["Title"]?.ToString()       ?? "—",
                            Category    = r["Category"]?.ToString()    ?? "—",
                            Level       = r["Level"]?.ToString()       ?? "—",
                            Description = r["Description"]?.ToString() ?? "",
                            AchieveDate = r["AchieveDate"] != DBNull.Value ? Convert.ToDateTime(r["AchieveDate"]).ToString("dd.MM.yyyy") : "—"
                        });
                AchievementsList.ItemsSource = list;
            }
            catch { }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private static string NullOrDash(object val)
            => val == DBNull.Value || val == null || string.IsNullOrWhiteSpace(val.ToString()) ? "—" : val.ToString();
        private static bool ToBool(object val)
            => val != DBNull.Value && val != null && Convert.ToBoolean(val);
    }

    public class ParentRow   { public string Relation, FullName, Phone, WorkPhone, Workplace, Position, Education; }
    public class GradeRow    { public string SubjectName, GradeType, GradeValue, GradeDate; }
    public class AttRow      { public string LessonDate, Subject, Status, Reason; }
    public class AchRow      { public string Title, Category, Level, Description, AchieveDate; }
    public class DocumentRow { public string Title, DocumentType, FileSize, UploadedAt, UploadedBy, Description; }
}
