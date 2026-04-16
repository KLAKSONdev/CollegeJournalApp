using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class SubjectEditDialog : Window
    {
        private readonly int? _subjectId;

        public SubjectEditDialog(int? subjectId)
        {
            InitializeComponent();
            _subjectId = subjectId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroups();
            LoadTeachers();
            PopulateSemesters();

            if (_subjectId.HasValue)
            {
                TxtTitle.Text = "Редактировать дисциплину";
                LoadSubjectData();
            }
            else
            {
                TxtTitle.Text = "Добавить дисциплину";
                CmbControl.SelectedIndex = 0;
                CmbSemester.SelectedIndex = 0;
            }

            // Wire up TextChanged for hours auto-sum
            TxtLecture.TextChanged += HoursChanged;
            TxtPractice.TextChanged += HoursChanged;
            TxtLab.TextChanged += HoursChanged;
            TxtSelf.TextChanged += HoursChanged;
        }

        private void PopulateSemesters()
        {
            CmbSemester.Items.Clear();
            for (int i = 1; i <= 10; i++)
                CmbSemester.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i.ToString() });
        }

        private void LoadGroups()
        {
            CmbGroup.Items.Clear();
            var dt = DatabaseHelper.ExecuteProcedure("sp_GetAllGroups", null);
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                var item = new ComboBoxItem
                {
                    Content = row["GroupName"]?.ToString(),
                    Tag = Convert.ToInt32(row["GroupId"])
                };
                CmbGroup.Items.Add(item);
            }
        }

        private void LoadTeachers()
        {
            CmbTeacher.Items.Clear();
            CmbTeacher.Items.Add(new ComboBoxItem { Content = "— Не назначен —", Tag = 0 });
            var dt = DatabaseHelper.ExecuteProcedure("sp_GetTeachersAll", null);
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                CmbTeacher.Items.Add(new ComboBoxItem
                {
                    Content = row["FullName"]?.ToString(),
                    Tag = Convert.ToInt32(row["TeacherId"])
                });
            }
            CmbTeacher.SelectedIndex = 0;
        }

        private void LoadSubjectData()
        {
            var dt = DatabaseHelper.ExecuteProcedure("sp_GetSubjectsAll", null);
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                if (Convert.ToInt32(row["SubjectId"]) != _subjectId.Value) continue;

                TxtName.Text = row["SubjectName"]?.ToString() ?? "";
                TxtLecture.Text  = row["HoursLecture"]?.ToString()  ?? "0";
                TxtPractice.Text = row["HoursPractice"]?.ToString() ?? "0";
                TxtLab.Text      = row["HoursLab"]?.ToString()      ?? "0";
                TxtSelf.Text     = row["HoursSelfStudy"]?.ToString() ?? "0";

                int groupId   = Convert.ToInt32(row["GroupId"]);
                int teacherId = row["TeacherId"] != DBNull.Value ? Convert.ToInt32(row["TeacherId"]) : 0;
                string sem    = row["Semester"]?.ToString() ?? "1";
                string ctrl   = row["ControlType"]?.ToString() ?? "";

                // Select Group
                foreach (ComboBoxItem it in CmbGroup.Items)
                    if (it.Tag is int gid && gid == groupId) { CmbGroup.SelectedItem = it; break; }

                // Select Teacher
                foreach (ComboBoxItem it in CmbTeacher.Items)
                    if (it.Tag is int tid && tid == teacherId) { CmbTeacher.SelectedItem = it; break; }

                // Select Semester
                foreach (ComboBoxItem it in CmbSemester.Items)
                    if (it.Content?.ToString() == sem) { CmbSemester.SelectedItem = it; break; }

                // Select ControlType
                foreach (ComboBoxItem it in CmbControl.Items)
                    if (it.Content?.ToString() == ctrl) { CmbControl.SelectedItem = it; break; }

                UpdateTotal();
                break;
            }
        }

        private void HoursChanged(object sender, TextChangedEventArgs e) => UpdateTotal();

        private void UpdateTotal()
        {
            int l = ParseHour(TxtLecture.Text);
            int p = ParseHour(TxtPractice.Text);
            int b = ParseHour(TxtLab.Text);
            int s = ParseHour(TxtSelf.Text);
            TxtTotalCalc.Text = (l + p + b + s).ToString();
        }

        private static int ParseHour(string s) =>
            int.TryParse(s, out int v) ? Math.Max(0, v) : 0;

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CmbGroup.SelectedItem == null)
            { MessageBox.Show("Выберите группу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            { MessageBox.Show("Введите название дисциплины.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CmbSemester.SelectedItem == null)
            { MessageBox.Show("Выберите семестр.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CmbControl.SelectedItem == null)
            { MessageBox.Show("Выберите форму контроля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            int groupId    = (int)((ComboBoxItem)CmbGroup.SelectedItem).Tag;
            var teacherTag = ((ComboBoxItem)CmbTeacher.SelectedItem)?.Tag;
            object teacherIdDb = (teacherTag is int tid && tid > 0) ? (object)tid : DBNull.Value;
            string semVal  = ((ComboBoxItem)CmbSemester.SelectedItem).Content?.ToString() ?? "1";
            string ctrl    = ((ComboBoxItem)CmbControl.SelectedItem).Content?.ToString() ?? "";
            int hoursL     = ParseHour(TxtLecture.Text);
            int hoursP     = ParseHour(TxtPractice.Text);
            int hoursB     = ParseHour(TxtLab.Text);
            int hoursS     = ParseHour(TxtSelf.Text);
            int hoursTotal = hoursL + hoursP + hoursB + hoursS;

            try
            {
                if (!_subjectId.HasValue)
                {
                    DatabaseHelper.ExecuteNonQuery("sp_AddSubject", new[]
                    {
                        new SqlParameter("@GroupId",        groupId),
                        new SqlParameter("@TeacherId",      teacherIdDb),
                        new SqlParameter("@SubjectName",    TxtName.Text.Trim()),
                        new SqlParameter("@HoursTotal",     hoursTotal),
                        new SqlParameter("@HoursLecture",   hoursL),
                        new SqlParameter("@HoursPractice",  hoursP),
                        new SqlParameter("@HoursLab",       hoursB),
                        new SqlParameter("@HoursSelfStudy", hoursS),
                        new SqlParameter("@Semester",       semVal),
                        new SqlParameter("@ControlType",    ctrl),
                        new SqlParameter("@AdminId",        SessionHelper.UserId)
                    });
                    MessageBox.Show("Дисциплина добавлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery("sp_UpdateSubject", new[]
                    {
                        new SqlParameter("@SubjectId",      _subjectId.Value),
                        new SqlParameter("@GroupId",        groupId),
                        new SqlParameter("@TeacherId",      teacherIdDb),
                        new SqlParameter("@SubjectName",    TxtName.Text.Trim()),
                        new SqlParameter("@HoursTotal",     hoursTotal),
                        new SqlParameter("@HoursLecture",   hoursL),
                        new SqlParameter("@HoursPractice",  hoursP),
                        new SqlParameter("@HoursLab",       hoursB),
                        new SqlParameter("@HoursSelfStudy", hoursS),
                        new SqlParameter("@Semester",       semVal),
                        new SqlParameter("@ControlType",    ctrl),
                        new SqlParameter("@AdminId",        SessionHelper.UserId)
                    });
                    MessageBox.Show("Дисциплина обновлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
