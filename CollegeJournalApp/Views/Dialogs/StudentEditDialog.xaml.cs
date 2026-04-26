using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class StudentEditDialog : Window
    {
        private readonly int? _studentId;

        public StudentEditDialog(int? studentId)
        {
            InitializeComponent();
            _studentId = studentId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroups();
            CmbGender.SelectedIndex = 0;
            CmbBasis.SelectedIndex  = 0;

            if (_studentId.HasValue)
            {
                TxtTitle.Text           = "Редактировать студента";
                PanelAccount.Visibility = Visibility.Collapsed;
                LoadStudentData();
            }
            else
            {
                TxtTitle.Text             = "Добавить студента";
                DpEnrollment.SelectedDate = DateTime.Today;
            }
        }

        private void LoadGroups()
        {
            CmbGroup.Items.Clear();
            CmbGroup.Items.Add(new ComboBoxItem { Content = "— Выберите группу —", Tag = 0 });
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAllGroups", null);
                foreach (DataRow row in dt.Rows)
                    CmbGroup.Items.Add(new ComboBoxItem
                    {
                        Content = row["GroupName"]?.ToString(),
                        Tag     = Convert.ToInt32(row["GroupId"])
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadGroups: " + ex.Message);
            }
            CmbGroup.SelectedIndex = 0;
        }

        private void LoadStudentData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentDetails",
                    new[] { new SqlParameter("@StudentId", _studentId.Value) });
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                TxtLastName.Text    = r["LastName"]?.ToString()    ?? "";
                TxtFirstName.Text   = r["FirstName"]?.ToString()   ?? "";
                TxtMiddleName.Text  = r["MiddleName"]?.ToString()  ?? "";
                TxtPhone.Text       = r["Phone"]?.ToString()       ?? "";
                TxtEmail.Text       = r["Email"]?.ToString()       ?? "";
                TxtAddress.Text     = r["Address"]?.ToString()     ?? "";
                TxtStudentCode.Text = r["StudentCode"]?.ToString() ?? "";
                ChkHeadman.IsChecked = r["IsHeadman"] != DBNull.Value && Convert.ToBoolean(r["IsHeadman"]);

                if (r["BirthDate"]     != DBNull.Value) DpBirthDate.SelectedDate  = Convert.ToDateTime(r["BirthDate"]);
                if (r["EnrollmentDate"]!= DBNull.Value) DpEnrollment.SelectedDate = Convert.ToDateTime(r["EnrollmentDate"]);

                CmbGender.SelectedIndex = r["Gender"]?.ToString() == "Женский" ? 1 : 0;
                CmbBasis.SelectedIndex  = r["StudyBasis"]?.ToString() == "Контракт" ? 1 : 0;

                var groupId = r["GroupId"] != DBNull.Value ? Convert.ToInt32(r["GroupId"]) : 0;
                foreach (ComboBoxItem item in CmbGroup.Items)
                    if (item.Tag is int id && id == groupId)
                    { CmbGroup.SelectedItem = item; break; }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных студента:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Авто-подстановка только при добавлении нового студента и если поле ещё не заполнено вручную
            if (_studentId.HasValue) return;

            if (!(CmbGroup.SelectedItem is ComboBoxItem item) ||
                !(item.Tag is int groupId) || groupId == 0)
            {
                TxtStudentCode.Text = "";
                return;
            }

            try
            {
                var row = DatabaseHelper.ExecuteSingleRow("sp_GetNextStudentCode",
                    new[] { new SqlParameter("@GroupId", groupId) });

                var code = row?["NextCode"]?.ToString() ?? "";
                TxtStudentCode.Text = code;
            }
            catch
            {
                TxtStudentCode.Text = "";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate(out int groupId, out string gender, out string basis, out DateTime birthDate))
                return;

            try
            {
                if (!_studentId.HasValue)
                {
                    // Добавление
                    var result = DatabaseHelper.ExecuteSingleRow("sp_AddStudent", new[]
                    {
                        new SqlParameter("@Login",          TxtLogin.Text.Trim()),
                        new SqlParameter("@PasswordHash",   HashPassword(PwdPassword.Password)),
                        new SqlParameter("@LastName",       TxtLastName.Text.Trim()),
                        new SqlParameter("@FirstName",      TxtFirstName.Text.Trim()),
                        new SqlParameter("@MiddleName",     ToDb(TxtMiddleName.Text)),
                        new SqlParameter("@Phone",          ToDb(TxtPhone.Text)),
                        new SqlParameter("@Email",          ToDb(TxtEmail.Text)),
                        new SqlParameter("@GroupId",        groupId),
                        new SqlParameter("@BirthDate",      birthDate),
                        new SqlParameter("@Gender",         gender),
                        new SqlParameter("@StudyBasis",     basis),
                        new SqlParameter("@StudentCode",    ToDb(TxtStudentCode.Text)),
                        new SqlParameter("@EnrollmentDate", DpEnrollment.SelectedDate.HasValue ? (object)DpEnrollment.SelectedDate.Value : DBNull.Value),
                        new SqlParameter("@AddedById",      SessionHelper.UserId)
                    });

                    if (result == null || Convert.ToInt32(result["StudentId"]) <= 0)
                    {
                        var rawMsg = result?["Message"]?.ToString() ?? "Не удалось добавить студента.";
                        MessageBox.Show(TranslateSpMessage(rawMsg), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int newId = Convert.ToInt32(result["StudentId"]);
                    SaveDetails(newId, groupId, gender, basis, birthDate);

                    MessageBox.Show("Студент успешно добавлен!", "Готово",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                }
                else
                {
                    // Редактирование
                    SaveDetails(_studentId.Value, groupId, gender, basis, birthDate);
                    MessageBox.Show("Данные студента обновлены!", "Готово",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                // ex.Message уже переведён в DatabaseHelper.TranslateSqlError
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool Validate(out int groupId, out string gender, out string basis, out DateTime birthDate)
        {
            groupId   = 0;
            gender    = "Мужской";
            basis     = "Бюджет";
            birthDate = DateTime.Today;

            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            {
                Show("Введите фамилию студента."); return false;
            }
            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            {
                Show("Введите имя студента."); return false;
            }
            if (!(CmbGroup.SelectedItem is ComboBoxItem g) || !(g.Tag is int gid) || gid == 0)
            {
                Show("Выберите группу из списка."); return false;
            }
            if (!DpBirthDate.SelectedDate.HasValue)
            {
                Show("Укажите дату рождения."); return false;
            }

            double age = (DateTime.Today - DpBirthDate.SelectedDate.Value).TotalDays / 365.25;
            if (age < 5 || age > 100)
            {
                Show("Некорректная дата рождения.\nДопустимый возраст: от 5 до 100 лет."); return false;
            }

            if (!_studentId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(TxtLogin.Text) || TxtLogin.Text.Trim().Length < 4)
                {
                    Show("Логин должен содержать не менее 4 символов."); return false;
                }
                if (TxtLogin.Text.Contains(" "))
                {
                    Show("Логин не должен содержать пробелы."); return false;
                }
                if (PwdPassword.Password.Length < 6)
                {
                    Show("Пароль слишком короткий.\n\nМинимальная длина — 6 символов.");
                    return false;
                }
            }

            groupId   = gid;
            gender    = (CmbGender.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Мужской";
            basis     = (CmbBasis.SelectedItem  as ComboBoxItem)?.Content?.ToString() ?? "Бюджет";
            birthDate = DpBirthDate.SelectedDate.Value;
            return true;
        }

        private void SaveDetails(int studentId, int groupId, string gender, string basis, DateTime birthDate)
        {
            DatabaseHelper.ExecuteNonQuery("sp_UpdateStudent", new[]
            {
                new SqlParameter("@StudentId",   studentId),
                new SqlParameter("@LastName",    TxtLastName.Text.Trim()),
                new SqlParameter("@FirstName",   TxtFirstName.Text.Trim()),
                new SqlParameter("@MiddleName",  ToDb(TxtMiddleName.Text)),
                new SqlParameter("@Phone",       ToDb(TxtPhone.Text)),
                new SqlParameter("@Email",       ToDb(TxtEmail.Text)),
                new SqlParameter("@Address",     ToDb(TxtAddress.Text)),
                new SqlParameter("@GroupId",     groupId),
                new SqlParameter("@BirthDate",   birthDate),
                new SqlParameter("@Gender",      gender),
                new SqlParameter("@StudyBasis",  basis),
                new SqlParameter("@StudentCode", ToDb(TxtStudentCode.Text)),
                new SqlParameter("@IsHeadman",   ChkHeadman.IsChecked == true),
                new SqlParameter("@UpdatedById", SessionHelper.UserId)
            });
        }

        private static string TranslateSpMessage(string msg)
        {
            if (msg.Contains("UQ_Users_Login") || (msg.Contains("duplicate key") && msg.Contains("Login")))
                return "Пользователь с таким логином уже существует.\nВыберите другой логин.";
            if (msg.Contains("UQ_Students_Code") || msg.Contains("StudentCode"))
                return "Студент с таким номером зачётной книжки уже существует.";
            if (msg.Contains("UQ_Students_OneHeadman"))
                return "В группе уже есть староста.";
            if (msg.Contains("duplicate key") || msg.Contains("UNIQUE"))
                return "Запись с такими данными уже существует в базе.";
            return msg;
        }

        private static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        private static object ToDb(string s)
            => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private static void Show(string msg)
            => MessageBox.Show(msg, "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
