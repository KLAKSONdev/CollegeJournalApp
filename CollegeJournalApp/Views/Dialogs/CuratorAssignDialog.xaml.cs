using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class CuratorAssignDialog : Window
    {
        private readonly int    _groupId;
        private readonly string _groupName;
        private readonly int?   _currentCuratorUserId;

        public CuratorAssignDialog(int groupId, string groupName, int? currentCuratorUserId)
        {
            InitializeComponent();
            _groupId              = groupId;
            _groupName            = groupName;
            _currentCuratorUserId = currentCuratorUserId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtGroupName.Text = $"Группа: {_groupName}";

            CmbTeacher.Items.Clear();
            CmbTeacher.Items.Add(new ComboBoxItem
            {
                Content = "— Снять куратора —",
                Tag     = (int?)null
            });

            try
            {
                // Загружаем всех преподавателей (Teacher + Curator)
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetTeachersForCurator",
                    new[] { new SqlParameter("@GroupId", _groupId) });
                foreach (DataRow row in dt.Rows)
                {
                    var item = new ComboBoxItem
                    {
                        Content = row["FullName"]?.ToString(),
                        Tag     = Convert.ToInt32(row["UserId"])
                    };
                    CmbTeacher.Items.Add(item);

                    // Выделяем текущего куратора
                    if (_currentCuratorUserId.HasValue &&
                        Convert.ToInt32(row["UserId"]) == _currentCuratorUserId.Value)
                        CmbTeacher.SelectedItem = item;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки преподавателей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (CmbTeacher.SelectedItem == null)
                CmbTeacher.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var selected = CmbTeacher.SelectedItem as ComboBoxItem;
            if (selected == null) { DialogResult = false; return; }

            // Tag = int (UserId) или null (снять)
            int? teacherUserId = selected.Tag as int?;

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_AssignCurator", new[]
                {
                    new SqlParameter("@GroupId",       _groupId),
                    new SqlParameter("@TeacherUserId", teacherUserId.HasValue ? (object)teacherUserId.Value : DBNull.Value),
                    new SqlParameter("@AdminId",       SessionHelper.UserId)
                });

                var msg = teacherUserId.HasValue
                    ? $"Куратор группы «{_groupName}» назначен.\nПри следующем входе преподаватель увидит интерфейс куратора."
                    : $"Куратор группы «{_groupName}» снят.";

                MessageBox.Show(msg, "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
