using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class ParentEditDialog : Window
    {
        private readonly int  _studentId;
        private readonly int? _parentId;   // null = добавление, value = редактирование

        public ParentEditDialog(int studentId, int? parentId = null)
        {
            InitializeComponent();
            _studentId = studentId;
            _parentId  = parentId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CmbRelation.SelectedIndex  = 0;
            CmbEducation.SelectedIndex = 0;

            if (_parentId.HasValue)
            {
                TxtTitle.Text = "Редактировать родителя";
                LoadData();
            }
            else
            {
                TxtTitle.Text = "Добавить родителя";
            }
        }

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentParents",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                if (dt == null) return;

                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["ParentId"]) != _parentId.Value) continue;

                    SelectComboByTag(CmbRelation,  Str(r["Relation"]));
                    SelectComboByTag(CmbEducation, Str(r["Education"]));

                    TxtLastName.Text    = Str(r["LastName"]);
                    TxtFirstName.Text   = Str(r["FirstName"]);
                    TxtMiddleName.Text  = Str(r["MiddleName"]);
                    TxtPhone.Text       = Str(r["Phone"]);
                    TxtWorkPhone.Text   = Str(r["WorkPhone"]);
                    TxtEmail.Text       = Str(r["Email"]);
                    TxtAddress.Text     = Str(r["Address"]);
                    TxtWorkplace.Text   = Str(r["Workplace"]);
                    TxtPosition.Text    = Str(r["Position"]);
                    TxtDepartment.Text  = Str(r["Department"]);

                    if (r["BirthDate"] != DBNull.Value)
                        DpBirthDate.SelectedDate = Convert.ToDateTime(r["BirthDate"]);

                    ChkMainContact.IsChecked    = ToBool(r["IsMainContact"]);
                    ChkDeceased.IsChecked       = ToBool(r["IsDeceased"]);
                    ChkParentalRights.IsChecked = ToBool(r["HasParentalRights"]);
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
            { Warn("Введите фамилию."); return; }
            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            { Warn("Введите имя."); return; }

            var relation = SelectedTag(CmbRelation);
            if (string.IsNullOrEmpty(relation))
            { Warn("Выберите степень родства."); return; }

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_SaveParent", new[]
                {
                    new SqlParameter("@ParentId",         _parentId.HasValue ? (object)_parentId.Value : DBNull.Value),
                    new SqlParameter("@StudentId",        _studentId),
                    new SqlParameter("@Relation",         relation),
                    new SqlParameter("@LastName",         TxtLastName.Text.Trim()),
                    new SqlParameter("@FirstName",        TxtFirstName.Text.Trim()),
                    new SqlParameter("@MiddleName",       ToDb(TxtMiddleName.Text)),
                    new SqlParameter("@BirthDate",        DpBirthDate.SelectedDate.HasValue
                                                          ? (object)DpBirthDate.SelectedDate.Value
                                                          : DBNull.Value),
                    new SqlParameter("@Phone",            ToDb(TxtPhone.Text)),
                    new SqlParameter("@WorkPhone",        ToDb(TxtWorkPhone.Text)),
                    new SqlParameter("@Email",            ToDb(TxtEmail.Text)),
                    new SqlParameter("@Address",          ToDb(TxtAddress.Text)),
                    new SqlParameter("@Workplace",        ToDb(TxtWorkplace.Text)),
                    new SqlParameter("@Position",         ToDb(TxtPosition.Text)),
                    new SqlParameter("@Department",       ToDb(TxtDepartment.Text)),
                    new SqlParameter("@Education",        ToDb(SelectedTag(CmbEducation))),
                    new SqlParameter("@IsMainContact",    ChkMainContact.IsChecked    == true),
                    new SqlParameter("@IsDeceased",       ChkDeceased.IsChecked       == true),
                    new SqlParameter("@HasParentalRights",ChkParentalRights.IsChecked == true),
                    new SqlParameter("@UpdatedById",      SessionHelper.UserId)
                });

                MessageBox.Show(_parentId.HasValue ? "Данные родителя обновлены!" : "Родитель добавлен!",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        // ── Вспомогательные методы ─────────────────────────────

        private static void SelectComboByTag(ComboBox cmb, string tag)
        {
            foreach (ComboBoxItem item in cmb.Items)
                if (item.Tag?.ToString() == tag)
                { cmb.SelectedItem = item; return; }
        }

        private static string SelectedTag(ComboBox cmb)
            => (cmb.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

        private static string Str(object val)
            => val == DBNull.Value || val == null ? "" : val.ToString().Trim();

        private static object ToDb(string s)
            => string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private static bool ToBool(object val)
            => val != DBNull.Value && val != null && Convert.ToBoolean(val);

        private static void Warn(string msg)
            => MessageBox.Show(msg, "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
