using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CollegeJournalApp.Database;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class AuditPage : Page
    {
        private List<AuditRow> _allRows = new List<AuditRow>();

        public AuditPage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => FetchAndDisplay();
        }

        private void FetchAndDisplay()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAuditLog",
                    new[] { new SqlParameter("@Limit", 500) });

                _allRows.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    _allRows.Add(new AuditRow
                    {
                        LogId        = Convert.ToInt32(row["LogId"]),
                        ActionAt     = row["ActionAt"] != DBNull.Value
                                       ? Convert.ToDateTime(row["ActionAt"]).ToString("dd.MM.yyyy HH:mm:ss") : "—",
                        UserFullName = row["UserFullName"]?.ToString() ?? "Система",
                        UserLogin    = row["UserLogin"]?.ToString()    ?? "—",
                        RoleRu       = GetRoleRu(row["RoleName"]?.ToString()),
                        ActionCode   = row["Action"]?.ToString()       ?? "",
                        ActionRu     = row["ActionRu"]?.ToString()     ?? "—",
                        Description  = row["Description"]?.ToString()  ?? "—",
                        TableName    = row["TableName"]?.ToString()    ?? "",
                        RecordId     = row["RecordId"] != DBNull.Value ? Convert.ToInt32(row["RecordId"]) : (int?)null,
                        IPAddress    = row["IPAddress"]?.ToString()    ?? "—"
                    });
                }

                TxtTotal.Text = $"— {_allRows.Count} записей";
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки журнала:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (AuditGrid == null) return;

            var filtered = _allRows.AsEnumerable();

            var search = TxtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(r =>
                    r.UserFullName.ToLower().Contains(search) ||
                    r.UserLogin.ToLower().Contains(search) ||
                    r.Description.ToLower().Contains(search));

            if (CmbAction.SelectedIndex > 0)
            {
                var af = (CmbAction.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                filtered = filtered.Where(r => r.ActionRu == af);
            }

            AuditGrid.ItemsSource = filtered.ToList();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => FetchAndDisplay();

        private void AuditGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(AuditGrid.SelectedItem is AuditRow row)) return;
            try
            {
                var win = new AuditDetailWindow(row.LogId) { Owner = Window.GetWindow(this) };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка открытия деталей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetRoleRu(string role)
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

        private void Filter_Changed(object sender, TextChangedEventArgs e)
        {

        }
    }

    public class AuditRow
    {
        public int    LogId        { get; set; }
        public string ActionAt     { get; set; }
        public string UserFullName { get; set; }
        public string UserLogin    { get; set; }
        public string RoleRu       { get; set; }
        public string ActionCode   { get; set; }
        public string ActionRu     { get; set; }
        public string Description  { get; set; }
        public string TableName    { get; set; }
        public int?   RecordId     { get; set; }
        public string IPAddress    { get; set; }
    }
}
