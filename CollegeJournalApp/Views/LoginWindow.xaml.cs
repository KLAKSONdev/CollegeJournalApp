using System;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            CheckDbStatus();
        }

        private void CheckDbStatus()
        {
            if (DatabaseHelper.TestConnection())
            {
                TxtDbStatus.Text = "● База данных подключена";
            }
            else
            {
                TxtDbStatus.Text = "● Нет подключения к БД";
                TxtDbStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnLogin_Click(sender, null);
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            var login    = TxtLogin.Text.Trim();
            var password = PwdPassword.Password;

            if (string.IsNullOrEmpty(login))
            {
                ShowError("Введите логин");
                TxtLogin.Focus();
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Введите пароль");
                PwdPassword.Focus();
                return;
            }

            var parameters = new[]
            {
                new SqlParameter("@Login",        login),
                new SqlParameter("@PasswordHash", HashPassword(password)),
                new SqlParameter("@IPAddress",    GetLocalIP())
            };

            var row = DatabaseHelper.ExecuteSingleRow("sp_Login", parameters);

            if (row == null)
            {
                ShowError("Ошибка подключения к базе данных");
                return;
            }

            int success = Convert.ToInt32(row["Success"]);

            if (success == 0)
            {
                ShowError(row["Message"]?.ToString() ?? "Ошибка авторизации");
                PwdPassword.Clear();
                PwdPassword.Focus();
                return;
            }

            // Сохраняем сессию
            SessionHelper.UserId    = Convert.ToInt32(row["UserId"]);
            SessionHelper.Login     = row["Login"]?.ToString();
            SessionHelper.LastName  = row["LastName"]?.ToString();
            SessionHelper.FirstName = row["FirstName"]?.ToString();
            SessionHelper.FullName  = $"{row["LastName"]} {row["FirstName"]}";
            SessionHelper.RoleName  = row["RoleName"]?.ToString();

            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow; // явно закрепляем MainWindow до Show
            mainWindow.Show();
            this.Close();
        }

        private void BtnLoginMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnLoginClose_Click(object sender, RoutedEventArgs e)
            => Close();


        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }

        private static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        private string GetLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
