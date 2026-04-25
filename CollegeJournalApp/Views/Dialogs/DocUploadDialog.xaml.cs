using System.Windows;
using System.Windows.Controls;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class DocUploadDialog : Window
    {
        public string DocTitle    { get; private set; }
        public string DocType     { get; private set; }
        public string Description { get; private set; }

        public DocUploadDialog(string fileName)
        {
            InitializeComponent();
            TxtFileName.Text = fileName;
            // Предзаполняем название именем файла без расширения
            TxtTitle.Text = System.IO.Path.GetFileNameWithoutExtension(fileName);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            var title = TxtTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название документа.", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTitle.Focus();
                return;
            }

            DocTitle    = title;
            DocType     = (CmbDocType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Прочее";
            Description = TxtDescription.Text.Trim();

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
