using System;
using System.Data;
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
                TxtTitle.Text = "Редактировать преподавателя";
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

                    TxtLastName.Text   = r["LastName"]?.ToString()   ?? "";
                    TxtFirstName.Text  = r["FirstName"]?.ToString()  ?? "";
                    TxtMiddleName.Text = r["MiddleName"]?.ToString() ?? "";
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
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            {
                MessageBox.Show("Введите имя преподавателя.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_teacherId.HasValue)
                {
                    DatabaseHelper.ExecuteNonQuery("sp_AddTeacher", new[]
                    {
                        new SqlParameter("@LastName",   TxtLastName.Text.Trim()),
                        new SqlParameter("@FirstName",  TxtFirstName.Text.Trim()),
                        new SqlParameter("@MiddleName", ToDb(TxtMiddleName.Text)),
                        new SqlParameter("@IsActive",   ChkActive.IsChecked == true),
                        new SqlParameter("@AdminId",    SessionHelper.UserId)
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

        private static object ToDb(string s)
            => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
