using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CollegeJournalApp.Database;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class AuditPage : Page
    {
        private List<AuditRow> _allRows = new List<AuditRow>();
        private DataGrid _grid;
        private TextBox _searchBox;
        private ComboBox _actionFilter;
        private TextBlock _totalLabel;

        public AuditPage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => LoadAudit();
        }

        private void LoadAudit()
        {
            // Находим элементы через имена из XAML
            _grid        = FindName("AuditGrid")   as DataGrid;
            _searchBox   = FindName("TxtSearch")   as TextBox;
            _actionFilter= FindName("CmbAction")   as ComboBox;
            _totalLabel  = FindName("TxtTotal")    as TextBlock;

            if (_grid == null)
            {
                // XAML не подключён — пересоздаём страницу программно
                BuildPageProgrammatically();
                return;
            }

            FetchAndDisplay();
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

                if (_totalLabel != null) _totalLabel.Text = $"— {_allRows.Count} записей";
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
            if (_grid == null) return;

            var filtered = _allRows.AsEnumerable();

            var search = _searchBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(r =>
                    r.UserFullName.ToLower().Contains(search) ||
                    r.UserLogin.ToLower().Contains(search) ||
                    r.Description.ToLower().Contains(search));

            if (_actionFilter?.SelectedIndex > 0)
            {
                var af = (_actionFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                filtered = filtered.Where(r => r.ActionRu == af);
            }

            _grid.ItemsSource = filtered.ToList();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => FetchAndDisplay();

        private void AuditGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(_grid?.SelectedItem is AuditRow row)) return;
            ShowDetail(row.LogId, row.Description);
        }

        private void ShowDetail(int logId, string header) // legacy
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAuditDetail",
                    new[] { new SqlParameter("@LogId", logId) });
                if (dt == null || dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("═══ ПОДРОБНАЯ ИНФОРМАЦИЯ ═══\n");
                sb.AppendLine($"Действие:     {r["ActionRu"]}");
                sb.AppendLine($"Дата и время: {(r["ActionAt"] != DBNull.Value ? Convert.ToDateTime(r["ActionAt"]).ToString("dd.MM.yyyy HH:mm:ss") : "—")}\n");
                sb.AppendLine("─── Пользователь ───");
                sb.AppendLine($"ФИО:          {r["UserFullName"]}");
                sb.AppendLine($"Логин:        {r["UserLogin"]}");
                sb.AppendLine($"Роль:         {GetRoleRu(r["RoleName"]?.ToString())}");
                sb.AppendLine($"IP-адрес:     {r["IPAddress"] ?? "—"}");

                if (!string.IsNullOrEmpty(r["TableName"]?.ToString()))
                {
                    sb.AppendLine("\n─── Данные ───");
                    sb.AppendLine($"Раздел:       {DatabaseHelper.TableRu(r["TableName"]?.ToString())}");
                    if (r["RecordId"] != DBNull.Value)
                        sb.AppendLine($"ID записи:    #{r["RecordId"]}");
                    if (r["RecordDescription"] != DBNull.Value && !string.IsNullOrEmpty(r["RecordDescription"]?.ToString()))
                        sb.AppendLine($"Запись:       {r["RecordDescription"]}");
                }

                if (r["ViewTarget"] != DBNull.Value && !string.IsNullOrEmpty(r["ViewTarget"]?.ToString()))
                    sb.AppendLine($"\nПросмотрено:  {r["ViewTarget"]}");

                if (r["OldValues"] != DBNull.Value && !string.IsNullOrEmpty(r["OldValues"]?.ToString()))
                    sb.AppendLine($"\n─── Было ───\n{r["OldValues"]}");

                if (r["NewValues"] != DBNull.Value && !string.IsNullOrEmpty(r["NewValues"]?.ToString()))
                    sb.AppendLine($"\n─── Стало ───\n{r["NewValues"]}");

                var win = new CollegeJournalApp.Views.AuditDetailWindow(logId);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки деталей:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Если XAML не подключён — строим UI полностью кодом
        private void BuildPageProgrammatically()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Заголовок
            var header = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness = new Thickness(0,0,0,1),
                Padding = new Thickness(24,14,24,14)
            };
            _totalLabel = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(96,94,92)) };
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Children.Add(new TextBlock { Text = "Журнал действий", FontSize = 18, FontWeight = FontWeights.SemiBold });
            headerPanel.Children.Add(_totalLabel);
            _totalLabel.Margin = new Thickness(10,0,0,2);
            header.Child = headerPanel;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // Тулбар
            var toolbar = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness = new Thickness(0,0,0,1),
                Padding = new Thickness(24,10,24,10)
            };
            var toolPanel = new StackPanel { Orientation = Orientation.Horizontal };

            _searchBox = new TextBox { Width = 220, Height = 30, Padding = new Thickness(8,6,8,6), FontSize = 12 };
            _searchBox.TextChanged += (s,e) => ApplyFilter();
            toolPanel.Children.Add(_searchBox);

            _actionFilter = new ComboBox { Width = 160, Height = 30, Margin = new Thickness(8,0,0,0), FontSize = 12 };
            _actionFilter.Items.Add(new ComboBoxItem { Content = "Все действия", IsSelected = true });
            _actionFilter.Items.Add(new ComboBoxItem { Content = "Вход в систему" });
            _actionFilter.Items.Add(new ComboBoxItem { Content = "Добавление записи" });
            _actionFilter.Items.Add(new ComboBoxItem { Content = "Изменение записи" });
            _actionFilter.Items.Add(new ComboBoxItem { Content = "Удаление записи" });
            _actionFilter.Items.Add(new ComboBoxItem { Content = "Просмотр данных" });
            _actionFilter.SelectedIndex = 0;
            _actionFilter.SelectionChanged += (s,e) => ApplyFilter();
            toolPanel.Children.Add(_actionFilter);

            var btnRefresh = new Button
            {
                Content = "Обновить", Height = 30, Padding = new Thickness(14,0,14,0),
                Margin = new Thickness(8,0,0,0), FontSize = 12, Cursor = System.Windows.Input.Cursors.Hand,
                Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(208,208,208))
            };
            btnRefresh.Click += BtnRefresh_Click;
            toolPanel.Children.Add(btnRefresh);

            toolPanel.Children.Add(new TextBlock
            {
                Text = "  Двойной клик — подробности",
                FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(161,159,157)),
                VerticalAlignment = VerticalAlignment.Center
            });

            toolbar.Child = toolPanel;
            Grid.SetRow(toolbar, 1);
            root.Children.Add(toolbar);

            // Таблица
            _grid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                RowBackground = Brushes.White,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(250,250,250)),
                HeadersVisibility = DataGridHeadersVisibility.Column,
                SelectionMode = DataGridSelectionMode.Single,
                CanUserResizeRows = false,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                RowHeight = 34,
                Margin = new Thickness(24,16,24,16)
            };
            _grid.MouseDoubleClick += AuditGrid_DoubleClick;

            _grid.Columns.Add(new DataGridTextColumn { Header = "Дата и время", Binding = new System.Windows.Data.Binding("ActionAt"),    Width = new DataGridLength(135) });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Пользователь", Binding = new System.Windows.Data.Binding("UserFullName"),Width = new DataGridLength(180) });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Роль",         Binding = new System.Windows.Data.Binding("RoleRu"),      Width = new DataGridLength(110) });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Действие",     Binding = new System.Windows.Data.Binding("ActionRu"),    Width = new DataGridLength(140) });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Описание",     Binding = new System.Windows.Data.Binding("Description"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            _grid.Columns.Add(new DataGridTextColumn { Header = "IP",           Binding = new System.Windows.Data.Binding("IPAddress"),   Width = new DataGridLength(110) });

            Grid.SetRow(_grid, 2);
            root.Children.Add(_grid);

            this.Content = root;
            FetchAndDisplay();
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
