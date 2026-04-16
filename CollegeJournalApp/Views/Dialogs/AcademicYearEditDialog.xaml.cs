using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class AcademicYearEditDialog : Window
    {
        private readonly int? _yearId;

        public AcademicYearEditDialog(int? yearId)
        {
            InitializeComponent();
            _yearId = yearId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_yearId.HasValue)
            {
                TxtTitle.Text = "Редактировать учебный год";
                LoadYearData();
            }
            else
            {
                TxtTitle.Text = "Добавить учебный год";
            }
        }

        private void LoadYearData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAcademicYears", null);
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["YearId"]) != _yearId.Value) continue;

                    TxtName.Text        = r["Title"]?.ToString() ?? "";
                    DpStart.SelectedDate = r["StartDate"] != DBNull.Value
                        ? (DateTime?)Convert.ToDateTime(r["StartDate"]) : null;
                    DpEnd.SelectedDate  = r["EndDate"] != DBNull.Value
                        ? (DateTime?)Convert.ToDateTime(r["EndDate"]) : null;
                    ChkCurrent.IsChecked = r["IsCurrent"] != DBNull.Value && Convert.ToBoolean(r["IsCurrent"]);
                    break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ChkCurrent_Changed(object sender, RoutedEventArgs e)
        {
            TxtCurrentHint.Visibility = ChkCurrent.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Введите название учебного года.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DpStart.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату начала учебного года.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DpEnd.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату окончания учебного года.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DpEnd.SelectedDate <= DpStart.SelectedDate)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!_yearId.HasValue)
                {
                    DatabaseHelper.ExecuteNonQuery("sp_AddAcademicYear", new[]
                    {
                        new SqlParameter("@Title",     TxtName.Text.Trim()),
                        new SqlParameter("@StartDate", DpStart.SelectedDate.Value),
                        new SqlParameter("@EndDate",   DpEnd.SelectedDate.Value),
                        new SqlParameter("@IsCurrent", ChkCurrent.IsChecked == true),
                        new SqlParameter("@AdminId",   SessionHelper.UserId)
                    });
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery("sp_UpdateAcademicYear", new[]
                    {
                        new SqlParameter("@YearId",    _yearId.Value),
                        new SqlParameter("@Title",     TxtName.Text.Trim()),
                        new SqlParameter("@StartDate", DpStart.SelectedDate.Value),
                        new SqlParameter("@EndDate",   DpEnd.SelectedDate.Value),
                        new SqlParameter("@IsCurrent", ChkCurrent.IsChecked == true),
                        new SqlParameter("@AdminId",   SessionHelper.UserId)
                    });
                }

                MessageBox.Show("Учебный год сохранён!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
