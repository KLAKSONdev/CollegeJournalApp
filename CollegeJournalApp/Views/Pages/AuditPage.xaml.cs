using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosedXML.Excel;
using CollegeJournalApp.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

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

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var rows = (AuditGrid.ItemsSource as List<AuditRow>) ?? new List<AuditRow>();
            if (rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title    = "Сохранить журнал аудита",
                Filter   = "Excel (*.xlsx)|*.xlsx",
                FileName = $"Журнал_аудита_{DateTime.Now:yyyy-MM-dd}"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Аудит");

                    // ── Шапка ──────────────────────────────────────────────
                    string[] headers = { "Дата и время", "Пользователь", "Логин",
                                         "Роль", "Действие", "Описание", "IP-адрес" };
                    for (int c = 0; c < headers.Length; c++)
                    {
                        var cell = ws.Cell(1, c + 1);
                        cell.Value          = headers[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0078D4");
                        cell.Style.Font.FontColor       = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.White;
                    }

                    // ── Данные ─────────────────────────────────────────────
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var r   = rows[i];
                        int row = i + 2;
                        bool alt = i % 2 == 1;

                        var values = new object[]
                        {
                            r.ActionAt, r.UserFullName, r.UserLogin,
                            r.RoleRu,   r.ActionRu,    r.Description, r.IPAddress
                        };

                        for (int c = 0; c < values.Length; c++)
                        {
                            var cell = ws.Cell(row, c + 1);
                            cell.Value = values[c]?.ToString() ?? "";
                            cell.Style.Fill.BackgroundColor =
                                alt ? XLColor.FromHtml("#F0F4FB") : XLColor.White;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E3E6EE");
                        }

                        // Цвет ячейки «Действие» по типу
                        var actionCell = ws.Cell(row, 5);
                        switch (r.ActionCode)
                        {
                            case "LOGIN":        actionCell.Style.Font.FontColor = XLColor.FromHtml("#107C10"); break;
                            case "CREATE":       actionCell.Style.Font.FontColor = XLColor.FromHtml("#0063B1"); break;
                            case "UPDATE":       actionCell.Style.Font.FontColor = XLColor.FromHtml("#7A4F00"); break;
                            case "SOFT_DELETE":  actionCell.Style.Font.FontColor = XLColor.FromHtml("#A4262C"); break;
                            case "ACCESS_DENIED":actionCell.Style.Font.FontColor = XLColor.FromHtml("#A4262C"); break;
                        }
                        actionCell.Style.Font.Bold = true;
                    }

                    // ── Оформление ─────────────────────────────────────────
                    ws.Columns().AdjustToContents();
                    ws.Column(6).Width = Math.Min(ws.Column(6).Width, 60); // Описание — не шире 60
                    ws.SheetView.FreezeRows(1);

                    wb.SaveAs(dlg.FileName);
                }

                MessageBox.Show($"Экспортировано {rows.Count} записей.", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
