using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class SocialEditDialog : Window
    {
        private readonly int _studentId;

        public SocialEditDialog(int studentId)
        {
            InitializeComponent();
            _studentId = studentId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CmbHealthGroup.SelectedIndex    = 0;
            CmbDisabilityGroup.SelectedIndex = 0;
            CmbFamilyStructure.SelectedIndex = 0;
            CmbHousing.SelectedIndex         = 0;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentSocial",
                    new[] { new SqlParameter("@StudentId", _studentId) });
                if (dt == null || dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                SelectComboByTag(CmbHealthGroup,     Str(r["HealthGroup"]));
                SelectComboByTag(CmbDisabilityGroup, Str(r["DisabilityGroup"]));
                SelectComboByTag(CmbFamilyStructure, Str(r["FamilyStructure"]));
                SelectComboByTag(CmbHousing,         Str(r["HousingCondition"]));

                TxtDisability.Text      = Str(r["Disability"]);
                TxtDisabilityCert.Text  = Str(r["DisabilityCertificate"]);
                TxtChronic.Text         = Str(r["ChronicDiseases"]);
                TxtSocialBenefits.Text  = Str(r["SocialBenefits"]);
                TxtPsych.Text           = Str(r["PsychologicalFeatures"]);
                TxtNotes.Text           = Str(r["AdditionalNotes"]);

                ChkOrphan.IsChecked           = ToBool(r["IsOrphan"]);
                ChkHalfOrphan.IsChecked       = ToBool(r["IsHalfOrphan"]);
                ChkLargeFamily.IsChecked      = ToBool(r["IsFromLargeFamily"]);
                ChkLowIncome.IsChecked        = ToBool(r["IsLowIncome"]);
                ChkSocialVulnerable.IsChecked = ToBool(r["IsSociallyVulnerable"]);
                ChkGuardianship.IsChecked     = ToBool(r["IsOnGuardianship"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Логика: группа инвалидности требует описания
            var disGroup = SelectedTag(CmbDisabilityGroup);
            var disText  = TxtDisability.Text.Trim();
            if (!string.IsNullOrEmpty(disGroup) && string.IsNullOrEmpty(disText))
            {
                MessageBox.Show("Укажите описание инвалидности при выбранной группе.",
                    "Проверьте данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_SaveStudentSocial", new[]
                {
                    new SqlParameter("@StudentId",             _studentId),
                    new SqlParameter("@HealthGroup",           ToDb(SelectedTag(CmbHealthGroup))),
                    new SqlParameter("@ChronicDiseases",       ToDb(TxtChronic.Text)),
                    new SqlParameter("@Disability",            ToDb(disText)),
                    new SqlParameter("@DisabilityGroup",       ToDb(disGroup)),
                    new SqlParameter("@DisabilityCertificate", ToDb(TxtDisabilityCert.Text)),
                    new SqlParameter("@FamilyStructure",       ToDb(SelectedTag(CmbFamilyStructure))),
                    new SqlParameter("@HousingCondition",      ToDb(SelectedTag(CmbHousing))),
                    new SqlParameter("@AdditionalNotes",       ToDb(TxtNotes.Text)),
                    new SqlParameter("@SocialBenefits",        ToDb(TxtSocialBenefits.Text)),
                    new SqlParameter("@PsychologicalFeatures", ToDb(TxtPsych.Text)),
                    new SqlParameter("@IsOrphan",              ChkOrphan.IsChecked           == true),
                    new SqlParameter("@IsHalfOrphan",          ChkHalfOrphan.IsChecked       == true),
                    new SqlParameter("@IsFromLargeFamily",     ChkLargeFamily.IsChecked      == true),
                    new SqlParameter("@IsLowIncome",           ChkLowIncome.IsChecked        == true),
                    new SqlParameter("@IsSociallyVulnerable",  ChkSocialVulnerable.IsChecked == true),
                    new SqlParameter("@IsOnGuardianship",      ChkGuardianship.IsChecked     == true),
                    new SqlParameter("@UpdatedById",           SessionHelper.UserId)
                });

                MessageBox.Show("Социальная карточка сохранена!", "Готово",
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
    }
}
