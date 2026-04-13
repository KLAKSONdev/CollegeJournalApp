using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class GroupEditDialog : Window
    {
        private readonly int? _groupId;
        private bool _loading = false;

        public GroupEditDialog(int? groupId)
        {
            InitializeComponent();
            _groupId = groupId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _loading = true;
            LoadCurators();
            CmbCourse.SelectedIndex = 0;
            UpdateSemesters(1, -1);
            _loading = false;

            TxtTitle.Text = _groupId.HasValue ? "Редактировать группу" : "Добавить группу";
            if (_groupId.HasValue) LoadGroupData();
        }

        private void LoadCurators()
        {
            CmbCurator.Items.Clear();
            CmbCurator.Items.Add(new ComboBoxItem { Content = "— Не назначен —", Tag = 0 });
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetCurators", null);
                foreach (DataRow row in dt.Rows)
                    CmbCurator.Items.Add(new ComboBoxItem
                    {
                        Content = row["FullName"]?.ToString(),
                        Tag     = Convert.ToInt32(row["UserId"])
                    });
            }
            catch { }
            CmbCurator.SelectedIndex = 0;
        }

        private void CmbCourse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            UpdateSemesters(CmbCourse.SelectedIndex + 1, -1);
        }

        private void UpdateSemesters(int course, int selectedSemester)
        {
            CmbSemester.Items.Clear();
            int s1 = course * 2 - 1;
            int s2 = course * 2;
            CmbSemester.Items.Add(new ComboBoxItem { Content = s1.ToString(), Tag = s1 });
            CmbSemester.Items.Add(new ComboBoxItem { Content = s2.ToString(), Tag = s2 });
            CmbSemester.SelectedIndex = selectedSemester == s2 ? 1 : 0;
        }

        private void LoadGroupData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetGroupDetails",
                    new[] { new SqlParameter("@GroupId", _groupId.Value) });
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                TxtGroupName.Text = r["GroupName"]?.ToString()     ?? "";
                TxtSpecialty.Text = r["Specialty"]?.ToString()     ?? "";
                TxtSpecCode.Text  = r["SpecialtyCode"]?.ToString() ?? "";

                int course   = r["Course"]   != DBNull.Value ? Convert.ToInt32(r["Course"])   : 1;
                int semester = r["Semester"] != DBNull.Value ? Convert.ToInt32(r["Semester"]) : course * 2 - 1;

                _loading = true;
                CmbCourse.SelectedIndex = Math.Max(0, course - 1);
                UpdateSemesters(course, semester);
                _loading = false;

                SetCombo(CmbEduForm,  r["EducationForm"]?.ToString());
                SetCombo(CmbEduBasis, r["EducationBasis"]?.ToString());

                if (r["CuratorId"] != DBNull.Value)
                {
                    int cid = Convert.ToInt32(r["CuratorId"]);
                    foreach (ComboBoxItem item in CmbCurator.Items)
                        if (item.Tag is int id && id == cid)
                        { CmbCurator.SelectedItem = item; break; }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных группы:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetCombo(ComboBox cmb, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (ComboBoxItem item in cmb.Items)
                if (item.Content?.ToString() == text)
                { cmb.SelectedItem = item; return; }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtGroupName.Text))
            {
                MessageBox.Show("Введите название группы.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(TxtSpecialty.Text))
            {
                MessageBox.Show("Введите наименование специальности.", "Проверьте данные",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            int course   = CmbCourse.SelectedIndex + 1;
            int semester = CmbSemester.SelectedItem is ComboBoxItem si && si.Tag is int sv ? sv : course * 2 - 1;

            object curatorId = DBNull.Value;
            if (CmbCurator.SelectedItem is ComboBoxItem sel && sel.Tag is int cid && cid > 0)
                curatorId = cid;

            var eduForm  = (CmbEduForm.SelectedItem  as ComboBoxItem)?.Content?.ToString() ?? "Очная";
            var eduBasis = (CmbEduBasis.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Бюджет";

            try
            {
                if (!_groupId.HasValue)
                {
                    int yearId = GetCurrentYearId();
                    DatabaseHelper.ExecuteNonQuery("sp_AddGroup", new[]
                    {
                        new SqlParameter("@YearId",        yearId),
                        new SqlParameter("@CuratorId",     curatorId),
                        new SqlParameter("@GroupName",     TxtGroupName.Text.Trim()),
                        new SqlParameter("@Specialty",     TxtSpecialty.Text.Trim()),
                        new SqlParameter("@SpecialtyCode", string.IsNullOrEmpty(TxtSpecCode.Text) ? (object)DBNull.Value : TxtSpecCode.Text.Trim()),
                        new SqlParameter("@Course",        (byte)course),
                        new SqlParameter("@Semester",      (byte)semester),
                        new SqlParameter("@EduForm",       eduForm),
                        new SqlParameter("@EduBasis",      eduBasis),
                        new SqlParameter("@AdminId",       SessionHelper.UserId)
                    });
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery("sp_UpdateGroup", new[]
                    {
                        new SqlParameter("@GroupId",       _groupId.Value),
                        new SqlParameter("@CuratorId",     curatorId),
                        new SqlParameter("@GroupName",     TxtGroupName.Text.Trim()),
                        new SqlParameter("@Specialty",     TxtSpecialty.Text.Trim()),
                        new SqlParameter("@SpecialtyCode", string.IsNullOrEmpty(TxtSpecCode.Text) ? (object)DBNull.Value : TxtSpecCode.Text.Trim()),
                        new SqlParameter("@Course",        (byte)course),
                        new SqlParameter("@Semester",      (byte)semester),
                        new SqlParameter("@EduForm",       eduForm),
                        new SqlParameter("@EduBasis",      eduBasis),
                        new SqlParameter("@AdminId",       SessionHelper.UserId)
                    });
                }

                MessageBox.Show("Группа сохранена!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetCurrentYearId()
        {
            var r = DatabaseHelper.ExecuteSingleRow("sp_GetCurrentYear", null);
            return r != null ? Convert.ToInt32(r["YearId"]) : 1;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
