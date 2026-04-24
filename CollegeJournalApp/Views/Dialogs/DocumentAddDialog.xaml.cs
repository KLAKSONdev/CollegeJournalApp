using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class DocumentAddDialog : Window
    {
        private readonly int _studentId;
        private string _selectedFilePath = "";

        // Папка для хранения файлов документов
        private static readonly string DocsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CollegeJournalApp", "Documents");

        public DocumentAddDialog(int studentId)
        {
            InitializeComponent();
            _studentId = studentId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CmbType.SelectedIndex = 0;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Выберите файл документа",
                Filter = "Документы|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.txt;*.jpg;*.jpeg;*.png" +
                         "|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            var fi = new FileInfo(dlg.FileName);
            if (fi.Length > 20 * 1024 * 1024)
            {
                MessageBox.Show("Файл слишком большой. Максимальный размер — 20 МБ.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _selectedFilePath   = dlg.FileName;
            TxtFilePath.Text    = fi.Name;
            TxtFileInfo.Text    = $"Размер: {FormatSize(fi.Length)}  |  {fi.FullName}";
            BtnClearFile.Visibility = Visibility.Visible;
        }

        private void BtnClearFile_Click(object sender, RoutedEventArgs e)
        {
            _selectedFilePath       = "";
            TxtFilePath.Text        = "Файл не выбран";
            TxtFileInfo.Text        = "";
            BtnClearFile.Visibility = Visibility.Collapsed;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTitle.Text))
            {
                MessageBox.Show("Введите название документа.",
                    "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string savedPath = "";
                string fileSize  = "";

                // Копируем файл если выбран
                if (!string.IsNullOrEmpty(_selectedFilePath) && File.Exists(_selectedFilePath))
                {
                    Directory.CreateDirectory(DocsFolder);
                    var ext      = Path.GetExtension(_selectedFilePath);
                    var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(_selectedFilePath)}";
                    savedPath    = Path.Combine(DocsFolder, fileName);
                    File.Copy(_selectedFilePath, savedPath, overwrite: true);
                    fileSize  = FormatSize(new FileInfo(savedPath).Length);
                }

                var docType = (CmbType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

                DatabaseHelper.ExecuteNonQuery("sp_AddDocument", new[]
                {
                    new SqlParameter("@StudentId",    _studentId),
                    new SqlParameter("@UploadedById", SessionHelper.UserId),
                    new SqlParameter("@Title",        TxtTitle.Text.Trim()),
                    new SqlParameter("@DocumentType", string.IsNullOrEmpty(docType)
                                                      ? (object)DBNull.Value : docType),
                    new SqlParameter("@FilePath",     string.IsNullOrEmpty(savedPath)
                                                      ? (object)DBNull.Value : savedPath),
                    new SqlParameter("@FileSize",     string.IsNullOrEmpty(fileSize)
                                                      ? (object)DBNull.Value : fileSize),
                    new SqlParameter("@Description",  string.IsNullOrWhiteSpace(TxtDescription.Text)
                                                      ? (object)DBNull.Value : TxtDescription.Text.Trim())
                });

                MessageBox.Show("Документ добавлен!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} МБ";
            if (bytes >= 1024)        return $"{bytes / 1024.0:F0} КБ";
            return $"{bytes} Б";
        }
    }
}
