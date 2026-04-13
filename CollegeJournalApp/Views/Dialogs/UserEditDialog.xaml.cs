using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class UserEditDialog : Window
    {
        private readonly int? _userId;

        public UserEditDialog(int? userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_userId.HasValue)
            {
                TxtTitle.Text            = "Редактировать пользователя";
                PanelPassword.Visibility = Visibility.Collapsed;
                TxtLogin.IsReadOnly      = true;
                TxtLogin.Opacity         = 0.6;
                LoadUserData();
            }
            else
            {
                TxtTitle.Text     = "Добавить пользователя";
                CmbRole.SelectedIndex = 3; // Student по умолчанию
            }
        }

        private void LoadUserData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetUserDetails",
                    new[] { new SqlParameter("@UserId", _userId.Value) });
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                TxtLogin.Text      = r["Login"]?.ToString()      ?? "";
                TxtLastName.Text   = r["LastName"]?.ToString()   ?? "";
                TxtFirstName.Text  = r["FirstName"]?.ToString()  ?? "";
                TxtMiddleName.Text = r["MiddleName"]?.ToString() ?? "";
                TxtPhone.Text      = r["Phone"]?.ToString()      ?? "";
                TxtEmail.Text      = r["Email"]?.ToString()      ?? "";

                var role = r["RoleName"]?.ToString() ?? "";
                foreach (ComboBoxItem item in CmbRole.Items)
                    if (item.Content?.ToString() == role)
                    { CmbRole.SelectedItem = item; break; }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
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
            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            {
                MessageBox.Show("Введите фамилию пользователя.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            {
                MessageBox.Show("Введите имя пользователя.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (CmbRole.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль пользователя.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!_userId.HasValue && PwdPassword.Password.Length < 32)
            {
                MessageBox.Show(
                    "Пароль слишком короткий.\n\n" +
                    "Минимальная длина — 32 символа.\n" +
                    "Пример: MyPassword2024MyPassword2024",
                    "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var roleName = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Student";

            try
            {
                if (!_userId.HasValue)
                {
                    DatabaseHelper.ExecuteNonQuery("sp_AddUser", new[]
                    {
                        new SqlParameter("@Login",        TxtLogin.Text.Trim()),
                        new SqlParameter("@PasswordHash", PwdPassword.Password),
                        new SqlParameter("@RoleName",     roleName),
                        new SqlParameter("@LastName",     TxtLastName.Text.Trim()),
                        new SqlParameter("@FirstName",    TxtFirstName.Text.Trim()),
                        new SqlParameter("@MiddleName",   ToDb(TxtMiddleName.Text)),
                        new SqlParameter("@Phone",        ToDb(TxtPhone.Text)),
                        new SqlParameter("@Email",        ToDb(TxtEmail.Text)),
                        new SqlParameter("@AdminId",      SessionHelper.UserId)
                    });
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery("sp_UpdateUser", new[]
                    {
                        new SqlParameter("@UserId",     _userId.Value),
                        new SqlParameter("@Login",      TxtLogin.Text.Trim()),
                        new SqlParameter("@RoleName",   roleName),
                        new SqlParameter("@LastName",   TxtLastName.Text.Trim()),
                        new SqlParameter("@FirstName",  TxtFirstName.Text.Trim()),
                        new SqlParameter("@MiddleName", ToDb(TxtMiddleName.Text)),
                        new SqlParameter("@Phone",      ToDb(TxtPhone.Text)),
                        new SqlParameter("@Email",      ToDb(TxtEmail.Text)),
                        new SqlParameter("@AdminId",    SessionHelper.UserId)
                    });
                }

                MessageBox.Show("Пользователь сохранён!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static object ToDb(string s)
            => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
