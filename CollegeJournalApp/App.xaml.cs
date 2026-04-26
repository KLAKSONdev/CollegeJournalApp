using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CollegeJournalApp.Views;

namespace CollegeJournalApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Перехват исключений в UI-потоке (WPF Dispatcher)
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Перехват исключений в фоновых потоках
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            MessageBox.Show(
                "Произошла непредвиденная ошибка:\n\n" + e.Exception.Message,
                "Ошибка — EduTrack Pro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true; // не даём упасть процессу
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogException(ex);
            MessageBox.Show(
                "Критическая ошибка приложения:\n\n" + (ex?.Message ?? e.ExceptionObject?.ToString()),
                "Ошибка — EduTrack Pro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void LogException(Exception ex)
        {
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EduTrack Pro", "error.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}\n\n");
            }
            catch { /* логирование не должно ронять приложение */ }
        }
    }
}
