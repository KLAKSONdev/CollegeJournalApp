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
    public partial class TeacherEditDialog : Window
    {
        private readonly int? _teacherId;

        public TeacherEditDialog(int? teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_teacherId.HasValue)
            {
                TxtTitle.Text            = "Редактировать преподавателя";
                PanelAccount.Visibility  = Visibility.Collapsed;
                LoadTeacherData();
            }
            else
            {
                TxtTitle.Text = "Добавить преподавателя";
            }
        }

        private void LoadTeacherData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetTeachersAll", null);
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["TeacherId"]) != _teacherId.Value) continue;

                    TxtLastName.Text    = r["LastName"]?.ToString()   ?? "";
                    TxtFirstName.Text   = r["FirstName"]?.ToString()  ?? "";
                    TxtMiddleName.Text  = r["MiddleName"]?.ToString() ?? "";
                    ChkActive.IsChecked = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]);
                    break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            {
                MessageBox.Show("Введите фамилию преподавателя.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            {
                MessageBox.Show("Введите имя преподавателя.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            // Валидация учётной записи (только при добавлении)
            if (!_teacherId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(TxtLogin.Text) || TxtLogin.Text.Trim().Length < 4)
                {
                    MessageBox.Show("Логин должен содержать не менее 4 символов.", "Проверьте данные",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                if (TxtLogin.Text.Contains(" "))
                {
                    MessageBox.Show("Логин не должен содержать пробелы.", "Проверьте данные",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
                if (PwdPassword.Password.Length < 6)
                {
                    MessageBox.Show("Минимальная длина пароля — 6 символов.", "Проверьте данные",
                        MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }
            }

            try
            {
                if (!_teacherId.HasValue)
                {
                    DatabaseHelper.ExecuteNonQuery("sp_AddTeacher", new[]
                    {
                        new SqlParameter("@LastName",     TxtLastName.Text.Trim()),
                        new SqlParameter("@FirstName",    TxtFirstName.Text.Trim()),
                        new SqlParameter("@MiddleName",   ToDb(TxtMiddleName.Text)),
                        new SqlParameter("@IsActive",     ChkActive.IsChecked == true),
                        new SqlParameter("@Login",        TxtLogin.Text.Trim()),
                        new SqlParameter("@PasswordHash", HashPassword(PwdPassword.Password)),
                        new SqlParameter("@Phone",        ToDb(TxtPhone.Text)),
                        new SqlParameter("@Email",        ToDb(TxtEmail.Text)),
                        new SqlParameter("@AdminId",      SessionHelper.UserId)
                    });
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery("sp_UpdateTeacher", new[]
                    {
                        new SqlParameter("@TeacherId",  _teacherId.Value),
                        new SqlParameter("@LastName",   TxtLastName.Text.Trim()),
                        new SqlParameter("@FirstName",  TxtFirstName.Text.Trim()),
                        new SqlParameter("@MiddleName", ToDb(TxtMiddleName.Text)),
                        new SqlParameter("@IsActive",   ChkActive.IsChecked == true),
                        new SqlParameter("@AdminId",    SessionHelper.UserId)
                    });
                }

                MessageBox.Show("Преподаватель сохранён!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
