using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views.Dialogs;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class AdminPage : Page
    {
        private List<AdminStudentRow> _students = new List<AdminStudentRow>();
        private List<AdminGroupRow>   _groups   = new List<AdminGroupRow>();
        private List<AdminUserRow>    _users    = new List<AdminUserRow>();

        public AdminPage()
        {
            InitializeComponent();
            // Отключаем кэширование — страница всегда пересоздаётся свежей
            KeepAlive = false;
            Loaded += (s, e) => LoadAll();
        }

        private void LoadAll()
        {
            LoadStudents();
            LoadGroups();
            LoadUsers();
        }

        // ═══ СТУДЕНТЫ ═══
        public void LoadStudents()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentsByRole", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", "Admin")
                });

                _students.Clear();
                int i = 1;
                foreach (DataRow row in dt.Rows)
                {
                    var dorm = row["DormitoryName"] != DBNull.Value
                        ? row["DormitoryName"].ToString() +
                          (row["RoomNumber"] != DBNull.Value ? ", к." + row["RoomNumber"] : "")
                        : "—";
                    bool isHead = row["IsHeadman"] != DBNull.Value && Convert.ToBoolean(row["IsHeadman"]);
                    _students.Add(new AdminStudentRow
                    {
                        Num         = i++,
                        StudentId   = Convert.ToInt32(row["StudentId"]),
                        FullName    = row["FullName"]?.ToString(),
                        GroupName   = row["GroupName"]?.ToString() ?? "—",
                        StudentCode = row["StudentCode"]?.ToString() ?? "—",
                        BirthDate   = row["BirthDate"] != DBNull.Value ? Convert.ToDateTime(row["BirthDate"]).ToString("dd.MM.yyyy") : "—",
                        Gender      = row["Gender"]?.ToString() ?? "—",
                        StudyBasis  = row["StudyBasis"]?.ToString() ?? "—",
                        Dormitory   = dorm,
                        Phone       = row["Phone"]?.ToString() ?? "—",
                        Status      = isHead ? "Староста" : "Студент"
                    });
                }

                // Применяем текущий фильтр поиска
                ApplyStudentFilter();
                TxtStudentsTotal.Text = $"— {_students.Count} записей";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки студентов:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyStudentFilter()
        {
            var q = TxtStudentSearch?.Text?.Trim().ToLower() ?? "";
            if (string.IsNullOrEmpty(q))
                StudentsAdminGrid.ItemsSource = new List<AdminStudentRow>(_students);
            else
                StudentsAdminGrid.ItemsSource = _students
                    .Where(s => (s.FullName?.ToLower().Contains(q) == true) ||
                                (s.GroupName?.ToLower().Contains(q) == true))
                    .ToList();
        }

        private void TxtStudentSearch_Changed(object sender, TextChangedEventArgs e)
            => ApplyStudentFilter();

        private void BtnAddStudent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new StudentEditDialog(null);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                LoadStudents(); // сразу обновляем
        }

        private void BtnEditStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsAdminGrid.SelectedItem is AdminStudentRow row))
            {
                MessageBox.Show("Выберите студента для редактирования.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            OpenStudentEdit(row.StudentId);
        }

        private void StudentsAdminGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StudentsAdminGrid.SelectedItem is AdminStudentRow row)
                OpenStudentEdit(row.StudentId);
        }

        private void OpenStudentEdit(int studentId)
        {
            var dlg = new StudentEditDialog(studentId);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                LoadStudents(); // сразу обновляем
        }

        private void BtnDeleteStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsAdminGrid.SelectedItem is AdminStudentRow row))
            {
                MessageBox.Show("Выберите студента для удаления.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить студента «{row.FullName}»?\n\nЗапись будет помечена как удалённая.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_SoftDelete", new[]
                    {
                        new SqlParameter("@TableName",   "Students"),
                        new SqlParameter("@RecordId",    row.StudentId),
                        new SqlParameter("@DeletedById", SessionHelper.UserId)
                    });
                    LoadStudents(); // сразу убираем из таблицы
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ═══ ГРУППЫ ═══
        public void LoadGroups()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAllGroups", null);
                _groups.Clear();
                foreach (DataRow row in dt.Rows)
                    _groups.Add(new AdminGroupRow
                    {
                        GroupId       = Convert.ToInt32(row["GroupId"]),
                        GroupName     = row["GroupName"]?.ToString()     ?? "—",
                        Specialty     = row["Specialty"]?.ToString()     ?? "—",
                        SpecialtyCode = row["SpecialtyCode"]?.ToString() ?? "—",
                        Course        = row["Course"]?.ToString()        ?? "—",
                        Semester      = row["Semester"]?.ToString()      ?? "—",
                        EduForm       = row["EducationForm"]?.ToString() ?? "—",
                        EduBasis      = row["EducationBasis"]?.ToString()?? "—",
                        CuratorName   = row["CuratorName"]?.ToString()  ?? "—",
                        StudentCount  = row["StudentCount"]?.ToString()  ?? "0"
                    });
                GroupsAdminGrid.ItemsSource = new List<AdminGroupRow>(_groups);
                TxtGroupsTotal.Text = $"— {_groups.Count} групп";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки групп:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new GroupEditDialog(null);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                LoadGroups();
        }

        private void BtnEditGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!(GroupsAdminGrid.SelectedItem is AdminGroupRow row))
            {
                MessageBox.Show("Выберите группу для редактирования.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            OpenGroupEdit(row.GroupId);
        }

        private void GroupsAdminGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GroupsAdminGrid.SelectedItem is AdminGroupRow row)
                OpenGroupEdit(row.GroupId);
        }

        private void OpenGroupEdit(int groupId)
        {
            var dlg = new GroupEditDialog(groupId);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                LoadGroups();
        }

        private void BtnDeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!(GroupsAdminGrid.SelectedItem is AdminGroupRow row))
            {
                MessageBox.Show("Выберите группу для удаления.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var result = MessageBox.Show(
                $"Удалить группу «{row.GroupName}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_SoftDelete", new[]
                    {
                        new SqlParameter("@TableName",   "Groups"),
                        new SqlParameter("@RecordId",    row.GroupId),
                        new SqlParameter("@DeletedById", SessionHelper.UserId)
                    });
                    LoadGroups();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ═══ ПОЛЬЗОВАТЕЛИ ═══
        public void LoadUsers()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAllUsers", null);
                _users.Clear();
                foreach (DataRow row in dt.Rows)
                    _users.Add(new AdminUserRow
                    {
                        UserId    = Convert.ToInt32(row["UserId"]),
                        Login     = row["Login"]?.ToString()    ?? "—",
                        FullName  = row["FullName"]?.ToString() ?? "—",
                        Role      = GetRoleRu(row["RoleName"]?.ToString()),
                        Phone     = row["Phone"]?.ToString()    ?? "—",
                        Email     = row["Email"]?.ToString()    ?? "—",
                        Status    = Convert.ToBoolean(row["IsActive"]) ? "Активен" : "Заблокирован",
                        LastLogin = row["LastLogin"] != DBNull.Value
                            ? Convert.ToDateTime(row["LastLogin"]).ToString("dd.MM.yyyy HH:mm") : "Никогда"
                    });
                UsersAdminGrid.ItemsSource = new List<AdminUserRow>(_users);
                TxtUsersTotal.Text = $"— {_users.Count} пользователей";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new UserEditDialog(null);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                LoadUsers();
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersAdminGrid.SelectedItem is AdminUserRow row))
            {
                MessageBox.Show("Выберите пользователя для редактирования.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            OpenUserEdit(row.UserId);
        }

        private void OpenUserEdit(int userId)
        {
            var dlg = new UserEditDialog(userId);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true)
                LoadUsers();
        }

        private void BtnToggleUser_Click(object sender, RoutedEventArgs e)
        {
            if (!(UsersAdminGrid.SelectedItem is AdminUserRow row))
            {
                MessageBox.Show("Выберите пользователя.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (row.UserId == SessionHelper.UserId)
            {
                MessageBox.Show("Нельзя заблокировать собственный аккаунт.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            bool isActive = row.Status == "Активен";
            var action = isActive ? "заблокировать" : "разблокировать";
            var result = MessageBox.Show(
                $"Вы хотите {action} пользователя «{row.FullName}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_ToggleUserActive", new[]
                    {
                        new SqlParameter("@UserId",  row.UserId),
                        new SqlParameter("@AdminId", SessionHelper.UserId)
                    });
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetRoleRu(string role)
        {
            switch (role)
            {
                case "Admin":   return "Администратор";
                case "Curator": return "Куратор";
                case "Headman": return "Староста";
                case "Student": return "Студент";
                default:        return role ?? "—";
            }
        }
    }

    public class AdminStudentRow
    {
        public int    Num { get; set; }
        public int    StudentId   { get; set; }
        public string FullName    { get; set; }
        public string GroupName   { get; set; }
        public string StudentCode { get; set; }
        public string BirthDate   { get; set; }
        public string Gender      { get; set; }
        public string StudyBasis  { get; set; }
        public string Dormitory   { get; set; }
        public string Phone       { get; set; }
        public string Status      { get; set; }
    }
    public class AdminGroupRow
    {
        public int    GroupId       { get; set; }
        public string GroupName     { get; set; }
        public string Specialty     { get; set; }
        public string SpecialtyCode { get; set; }
        public string Course        { get; set; }
        public string Semester      { get; set; }
        public string EduForm       { get; set; }
        public string EduBasis      { get; set; }
        public string CuratorName   { get; set; }
        public string StudentCount  { get; set; }
    }
    public class AdminUserRow
    {
        public int    UserId    { get; set; }
        public string Login     { get; set; }
        public string FullName  { get; set; }
        public string Role      { get; set; }
        public string Phone     { get; set; }
        public string Email     { get; set; }
        public string Status    { get; set; }
        public string LastLogin { get; set; }
    }
}
