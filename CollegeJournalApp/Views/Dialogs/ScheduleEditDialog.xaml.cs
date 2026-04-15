using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class ScheduleEditDialog : Window
    {
        public bool Saved { get; private set; } = false;

        private readonly int? _scheduleId;   // null = добавление, не null = редактирование
        private int?          _pendingSubjectId; // ожидает выбора после загрузки предметов

        // ── ADD mode ──────────────────────────────────────────────────────────
        public ScheduleEditDialog(int? prefillGroupId = null,
                                  int? prefillDayNum  = null,
                                  int? prefillLesson  = null)
        {
            InitializeComponent();
            _scheduleId = null;
            TxtTitle.Text = "Добавить пару";
            Title         = "Добавить пару";

            LoadGroups(prefillGroupId);

            if (prefillDayNum.HasValue)
                SelectComboByTag(CmbDay, prefillDayNum.Value);

            if (prefillLesson.HasValue)
                SelectComboByTag(CmbLesson, prefillLesson.Value);
        }

        // ── EDIT mode ─────────────────────────────────────────────────────────
        public ScheduleEditDialog(int scheduleId, int groupId, int dayNum,
                                  int lessonNum, int subjectId,
                                  string classroom, string weekType)
        {
            InitializeComponent();
            _scheduleId = scheduleId;
            TxtTitle.Text = "Редактировать пару";
            Title         = "Редактировать пару";

            _pendingSubjectId  = subjectId;

            LoadGroups(groupId);
            SelectComboByTag(CmbDay,    dayNum);
            SelectComboByTag(CmbLesson, lessonNum);

            TxtClassroom.Text = classroom ?? "";
            SelectComboByContent(CmbWeekType, weekType ?? "Обе");
        }

        // ── Загрузка групп ────────────────────────────────────────────────────

        private void LoadGroups(int? selectGroupId)
        {
            CmbGroup.Items.Clear();
            CmbGroup.Items.Add(new ComboBoxItem { Content = "— Выберите группу —", Tag = 0 });

            var dt = DatabaseHelper.ExecuteProcedure("sp_GetAllGroups", null);
            foreach (DataRow r in dt.Rows)
            {
                var item = new ComboBoxItem
                {
                    Content = r["GroupName"]?.ToString(),
                    Tag     = Convert.ToInt32(r["GroupId"])
                };
                CmbGroup.Items.Add(item);
            }

            CmbGroup.SelectedIndex = 0;

            if (selectGroupId.HasValue && selectGroupId.Value > 0)
                SelectComboByTag(CmbGroup, selectGroupId.Value);
        }

        // ── Загрузка предметов при выборе группы ─────────────────────────────

        private void CmbGroup_Changed(object sender, SelectionChangedEventArgs e)
        {
            int groupId = (int)((CmbGroup.SelectedItem as ComboBoxItem)?.Tag ?? 0);
            CmbSubject.Items.Clear();
            TxtTeacher.Text = "— выберите предмет —";

            if (groupId == 0) return;

            CmbSubject.Items.Add(new ComboBoxItem { Content = "— Выберите предмет —", Tag = 0 });

            var dt = DatabaseHelper.ExecuteProcedure("sp_GetGroupSubjects",
                new[] { new SqlParameter("@GroupId", groupId) });

            foreach (DataRow r in dt.Rows)
            {
                CmbSubject.Items.Add(new ComboBoxItem
                {
                    Content = r["SubjectName"]?.ToString(),
                    Tag     = new SubjectInfo
                    {
                        SubjectId   = Convert.ToInt32(r["SubjectId"]),
                        TeacherName = r["TeacherName"]?.ToString() ?? "—"
                    }
                });
            }

            CmbSubject.SelectedIndex = 0;

            // Восстановить выбор при редактировании
            if (_pendingSubjectId.HasValue)
            {
                foreach (ComboBoxItem item in CmbSubject.Items)
                {
                    if (item.Tag is SubjectInfo si && si.SubjectId == _pendingSubjectId.Value)
                    {
                        item.IsSelected     = true;
                        _pendingSubjectId   = null;
                        break;
                    }
                }
            }
        }

        // ── Автозаполнение преподавателя при выборе предмета ─────────────────

        private void CmbSubject_Changed(object sender, SelectionChangedEventArgs e)
        {
            var info = (CmbSubject.SelectedItem as ComboBoxItem)?.Tag as SubjectInfo;
            TxtTeacher.Text = info != null ? info.TeacherName : "— выберите предмет —";
        }

        // ── Сохранение ────────────────────────────────────────────────────────

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            int groupId = TagToInt((CmbGroup.SelectedItem  as ComboBoxItem)?.Tag);
            int dayNum  = TagToInt((CmbDay.SelectedItem    as ComboBoxItem)?.Tag);
            int lesson  = TagToInt((CmbLesson.SelectedItem as ComboBoxItem)?.Tag);
            var subjInfo = (CmbSubject.SelectedItem as ComboBoxItem)?.Tag as SubjectInfo;
            int subjectId = subjInfo?.SubjectId ?? 0;

            if (groupId == 0 || dayNum == 0 || lesson == 0 || subjectId == 0)
            {
                MessageBox.Show("Заполните все обязательные поля: Группа, День, Пара, Предмет.",
                    "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string classroom = TxtClassroom.Text.Trim();
            string weekType  = (CmbWeekType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Обе";

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@ScheduleId",   _scheduleId.HasValue ? (object)_scheduleId.Value : DBNull.Value),
                    new SqlParameter("@GroupId",       groupId),
                    new SqlParameter("@DayOfWeek",     (byte)dayNum),
                    new SqlParameter("@LessonNumber",  (byte)lesson),
                    new SqlParameter("@SubjectId",     subjectId),
                    new SqlParameter("@Classroom",     string.IsNullOrEmpty(classroom) ? (object)DBNull.Value : classroom),
                    new SqlParameter("@WeekType",      weekType)
                };

                DatabaseHelper.ExecuteNonQuery("sp_SaveScheduleItem", parameters);
                Saved = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        // ── Вспомогательные методы ────────────────────────────────────────────

        // Tag в XAML хранится как string, в code-behind — как int; метод обрабатывает оба случая
        private static int TagToInt(object tag)
        {
            if (tag is int i) return i;
            if (tag is string s && int.TryParse(s, out int parsed)) return parsed;
            return 0;
        }

        private static void SelectComboByTag(ComboBox cmb, int tag)
        {
            foreach (ComboBoxItem item in cmb.Items)
            {
                // Tag may be int (set in code-behind) or string (set in XAML)
                if (item.Tag is int t && t == tag)
                { item.IsSelected = true; return; }
                if (item.Tag is string s && s == tag.ToString())
                { item.IsSelected = true; return; }
            }
        }

        private static void SelectComboByContent(ComboBox cmb, string content)
        {
            foreach (ComboBoxItem item in cmb.Items)
            {
                if (item.Content?.ToString() == content)
                { item.IsSelected = true; return; }
            }
        }

        private class SubjectInfo
        {
            public int    SubjectId   { get; set; }
            public string TeacherName { get; set; }
        }
    }
}
