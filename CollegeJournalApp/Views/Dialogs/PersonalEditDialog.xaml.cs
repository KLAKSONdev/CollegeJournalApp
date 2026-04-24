using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class PersonalEditDialog : Window
    {
        private readonly int _studentId;

        public PersonalEditDialog(int studentId)
        {
            InitializeComponent();
            _studentId = studentId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CmbPrevSchoolType.SelectedIndex = 0;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentDetails",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                if (dt == null || dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                TxtBirthPlace.Text       = Str(r["BirthPlace"]);
                TxtCitizenship.Text      = Str(r["Citizenship"]);
                TxtAddress.Text          = Str(r["Address"]);
                TxtSnils.Text            = Str(r["SNILSNumber"]);
                TxtPassportSeries.Text   = Str(r["PassportSeries"]);
                TxtPassportNumber.Text   = Str(r["PassportNumber"]);
                TxtPassportIssuedBy.Text = Str(r["PassportIssuedBy"]);
                TxtPrevSchool.Text       = Str(r["PreviousSchool"]);

                if (r["PassportIssuedDate"] != DBNull.Value)
                    DpPassportDate.SelectedDate = Convert.ToDateTime(r["PassportIssuedDate"]);

                var prevType = Str(r["PreviousSchoolType"]);
                foreach (ComboBoxItem item in CmbPrevSchoolType.Items)
                    if (item.Tag?.ToString() == prevType)
                    { CmbPrevSchoolType.SelectedItem = item; break; }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var series = TxtPassportSeries.Text.Trim();
            var number = TxtPassportNumber.Text.Trim();

            // Базовая проверка паспорта — либо оба заполнены, либо оба пусты
            if (series.Length > 0 && number.Length == 0)
            { Warn("Укажите номер паспорта (6 цифр)."); return; }
            if (number.Length > 0 && series.Length == 0)
            { Warn("Укажите серию паспорта (4 цифры)."); return; }
            if (series.Length > 0 && (series.Length != 4 || !IsDigits(series)))
            { Warn("Серия паспорта должна содержать ровно 4 цифры."); return; }
            if (number.Length > 0 && (number.Length != 6 || !IsDigits(number)))
            { Warn("Номер паспорта должен содержать ровно 6 цифр."); return; }

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_UpdateStudentPersonal", new[]
                {
                    new SqlParameter("@StudentId",          _studentId),
                    new SqlParameter("@BirthPlace",         ToDb(TxtBirthPlace.Text)),
                    new SqlParameter("@Citizenship",        ToDb(TxtCitizenship.Text)),
                    new SqlParameter("@Address",            ToDb(TxtAddress.Text)),
                    new SqlParameter("@SNILSNumber",        ToDb(TxtSnils.Text)),
                    new SqlParameter("@PassportSeries",     ToDb(series)),
                    new SqlParameter("@PassportNumber",     ToDb(number)),
                    new SqlParameter("@PassportIssuedBy",   ToDb(TxtPassportIssuedBy.Text)),
                    new SqlParameter("@PassportIssuedDate", DpPassportDate.SelectedDate.HasValue
                                                            ? (object)DpPassportDate.SelectedDate.Value
                                                            : DBNull.Value),
                    new SqlParameter("@PreviousSchool",     ToDb(TxtPrevSchool.Text)),
                    new SqlParameter("@PreviousSchoolType", ToDb(SelectedTag(CmbPrevSchoolType))),
                    new SqlParameter("@UpdatedById",        SessionHelper.UserId)
                });

                MessageBox.Show("Данные успешно сохранены!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        // ── Вспомогательные методы ─────────────────────────────

        private static string Str(object val)
            => val == DBNull.Value || val == null ? "" : val.ToString().Trim();

        private static object ToDb(string s)
            => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private static string SelectedTag(ComboBox cmb)
            => (cmb.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

        private static bool IsDigits(string s)
        {
            foreach (char c in s) if (!char.IsDigit(c)) return false;
            return true;
        }

        private static void Warn(string msg)
            => MessageBox.Show(msg, "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
