using System;
using System.Data;
using System.Windows;
using System.Windows.Media;
using CollegeJournalApp.Database;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views
{
    public partial class AuditDetailWindow : Window
    {
        private readonly int _logId;

        public AuditDetailWindow(int logId)
        {
            InitializeComponent();
            _logId = logId;
            Loaded += (s, e) => LoadDetail();
        }

        private void LoadDetail()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAuditDetail",
                    new[] { new SqlParameter("@LogId", _logId) });

                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("Запись не найдена.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                var r = dt.Rows[0];

                var actionCode = r["Action"]?.ToString() ?? "";
                var actionRu   = r["ActionRu"]?.ToString() ?? actionCode;
                var dateTime   = r["ActionAt"] != DBNull.Value
                    ? Convert.ToDateTime(r["ActionAt"]).ToString("dd MMMM yyyy, HH:mm:ss",
                        new System.Globalization.CultureInfo("ru-RU"))
                    : "—";

                // Заголовок
                Title = $"Действие #{_logId} — {actionRu}";
                TxtActionRu.Text = actionRu;
                TxtDateTime.Text = dateTime;

                // Цвет и иконка по типу действия
                SetActionStyle(actionCode);

                // Пользователь
                var fullName = r["UserFullName"]?.ToString() ?? "Система";
                TxtUserFullName.Text = fullName;
                TxtAvatar.Text = fullName.Length > 0 ? fullName.Substring(0, 1).ToUpper() : "?";
                TxtLogin.Text  = "@" + (r["UserLogin"]?.ToString() ?? "—");
                TxtRole.Text   = GetRoleRu(r["RoleName"]?.ToString());

                // Детали
                var tableName = r["TableName"]?.ToString() ?? "";
                TxtTable.Text = string.IsNullOrEmpty(tableName) ? "—" : DatabaseHelper.TableRu(tableName);

                var recordDesc = r["RecordDescription"] != DBNull.Value
                    ? r["RecordDescription"].ToString() : "";
                var recordId = r["RecordId"] != DBNull.Value
                    ? $"  (ID: #{r["RecordId"]})" : "";
                TxtRecord.Text = string.IsNullOrEmpty(recordDesc)
                    ? (r["RecordId"] != DBNull.Value ? $"#{r["RecordId"]}" : "—")
                    : recordDesc + recordId;

                TxtIP.Text = r["IPAddress"]?.ToString() ?? "—";

                // Просмотренные данные
                var viewTarget = r["ViewTarget"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(viewTarget))
                {
                    LblView.Visibility = Visibility.Visible;
                    TxtView.Visibility = Visibility.Visible;
                    TxtView.Text = viewTarget;
                }

                // Блок изменений
                var oldVal = r["OldValues"]?.ToString() ?? "";
                var newVal = r["NewValues"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(oldVal) || !string.IsNullOrEmpty(newVal))
                {
                    BdrChanges.Visibility = Visibility.Visible;
                    TxtOld.Text = string.IsNullOrEmpty(oldVal) ? "—" : oldVal;
                    TxtNew.Text = string.IsNullOrEmpty(newVal) ? "—" : newVal;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки деталей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetActionStyle(string action)
        {
            Color bg;
            string icon;
            switch (action)
            {
                case "LOGIN":
                    bg = Color.FromRgb(16, 124, 16); icon = "→"; break;
                case "LOGOUT":
                    bg = Color.FromRgb(96, 94, 92);  icon = "←"; break;
                case "CREATE":
                    bg = Color.FromRgb(0, 120, 212);  icon = "+"; break;
                case "UPDATE":
                    bg = Color.FromRgb(202, 80, 16);  icon = "✎"; break;
                case "SOFT_DELETE":
                    bg = Color.FromRgb(209, 52, 56);  icon = "✕"; break;
                case "RESTORE":
                    bg = Color.FromRgb(0, 99, 177);   icon = "↺"; break;
                case "VIEW":
                    bg = Color.FromRgb(0, 120, 212);  icon = "◉"; break;
                case "ACCESS_DENIED":
                    bg = Color.FromRgb(209, 52, 56);  icon = "✗"; break;
                default:
                    bg = Color.FromRgb(96, 94, 92);   icon = "●"; break;
            }
            BdrIcon.Background = new SolidColorBrush(bg);
            TxtIcon.Text = icon;
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

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
