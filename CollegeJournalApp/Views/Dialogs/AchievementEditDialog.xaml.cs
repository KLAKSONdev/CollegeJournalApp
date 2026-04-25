using System;
using System.Windows;
using System.Windows.Controls;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class AchievementEditDialog : Window
    {
        // Результат диалога — null если отменили
        public AchievementResult Result { get; private set; }

        public AchievementEditDialog(string studentName, AchievementResult existing = null)
        {
            InitializeComponent();

            TxtStudentName.Text = studentName;

            if (existing != null)
            {
                // Режим редактирования
                TxtWindowTitle.Text   = "Редактировать достижение";
                TxtTitle.Text         = existing.Title;
                TxtDescription.Text   = existing.Description;
                TxtDocNumber.Text     = existing.DocumentNumber;
                DtAchieve.SelectedDate = existing.AchieveDate;

                SelectComboItem(CmbCategory, existing.Category);
                SelectComboItem(CmbLevel,    existing.Level);
            }
        }

        private void SelectComboItem(ComboBox cmb, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            foreach (ComboBoxItem item in cmb.Items)
            {
                if (item.Content?.ToString() == value)
                {
                    item.IsSelected = true;
                    return;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var title = TxtTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название достижения.", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTitle.Focus();
                return;
            }

            Result = new AchievementResult
            {
                Title          = title,
                Category       = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Другое",
                Level          = (CmbLevel.SelectedItem    as ComboBoxItem)?.Content?.ToString() ?? "",
                Description    = TxtDescription.Text.Trim(),
                DocumentNumber = TxtDocNumber.Text.Trim(),
                AchieveDate    = DtAchieve.SelectedDate
            };

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }

    public class AchievementResult
    {
        public int?     AchievementId  { get; set; }  // null = новое
        public string   Title          { get; set; }
        public string   Category       { get; set; }
        public string   Level          { get; set; }
        public string   Description    { get; set; }
        public DateTime? AchieveDate   { get; set; }
        public string   DocumentNumber { get; set; }
    }
}
