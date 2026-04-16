using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class DormitoryEditDialog : Window
    {
        private readonly int? _dormId;

        public DormitoryEditDialog(int? dormId)
        {
            InitializeComponent();
            _dormId = dormId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_dormId.HasValue)
            {
                TxtTitle.Text = "Редактировать общежитие";
                LoadDormitoryData();
            }
            else
            {
                TxtTitle.Text = "Добавить общежитие";
            }
        }

        private void LoadDormitoryData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetDormitories", null);
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["DormitoryId"]) != _dormId.Value) continue;

                    TxtName.Text       = r["Name"]?.ToString()          ?? "";
                    TxtAddress.Text    = r["Address"]?.ToString()       ?? "";
                    TxtCommandant.Text = r["CommandantName"]?.ToString() ?? "";
                    TxtPhone.Text      = r["Phone"]?.ToString()         ?? "";
                    TxtRooms.Text      = r["TotalRooms"] != DBNull.Value
                        ? Convert.ToInt32(r["TotalRooms"]).ToString() : "0";
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
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Введите название общежития.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtRooms.Text.Trim(), out int rooms) || rooms < 0)
            {
                MessageBox.Show("Кол-во комнат должно быть целым числом не меньше 0.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_dormId.HasValue)
                {
                    DatabaseHelper.ExecuteNonQuery("sp_AddDormitory", new[]
                    {
                        new SqlParameter("@Name",           TxtName.Text.Trim()),
                        new SqlParameter("@Address",        ToDb(TxtAddress.Text)),
                        new SqlParameter("@CommandantName", ToDb(TxtCommandant.Text)),
                        new SqlParameter("@Phone",          ToDb(TxtPhone.Text)),
                        new SqlParameter("@TotalRooms",     rooms),
                        new SqlParameter("@AdminId",        SessionHelper.UserId)
                    });
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery("sp_UpdateDormitory", new[]
                    {
                        new SqlParameter("@DormitoryId",    _dormId.Value),
                        new SqlParameter("@Name",           TxtName.Text.Trim()),
                        new SqlParameter("@Address",        ToDb(TxtAddress.Text)),
                        new SqlParameter("@CommandantName", ToDb(TxtCommandant.Text)),
                        new SqlParameter("@Phone",          ToDb(TxtPhone.Text)),
                        new SqlParameter("@TotalRooms",     rooms),
                        new SqlParameter("@AdminId",        SessionHelper.UserId)
                    });
                }

                MessageBox.Show("Общежитие сохранено!", "Готово",
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
