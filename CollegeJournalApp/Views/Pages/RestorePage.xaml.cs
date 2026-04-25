using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class RestorePage : Page
    {
        private List<DeletedRow> _allRows  = new List<DeletedRow>();
        private string           _activeFilter = "All";

        public RestorePage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) =>
            {
                UpdateChipStyles();
                FetchAndDisplay();
            };
        }

        // ── Загрузка данных ────────────────────────────────────────────────

        private void FetchAndDisplay()
        {
            try
            {
                // Передаём null = все таблицы
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetDeletedRecords",
                    new[] { new SqlParameter("@TableFilter", DBNull.Value) });

                _allRows.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    var deletedAt = row["DeletedAt"] != DBNull.Value
                        ? (DateTime?)Convert.ToDateTime(row["DeletedAt"])
                        : null;

                    _allRows.Add(new DeletedRow
                    {
                        TableName     = row["TableName"]?.ToString()    ?? "",
                        TableNameRu   = GetTableRu(row["TableName"]?.ToString()),
                        RecordId      = Convert.ToInt32(row["RecordId"]),
                        RecordName    = row["RecordName"]?.ToString()   ?? "—",
                        ExtraInfo     = row["ExtraInfo"]?.ToString()    ?? "—",
                        GroupInfo     = row["GroupInfo"]?.ToString()    ?? "—",
                        DeletedAt     = deletedAt,
                        DeletedAtStr  = deletedAt.HasValue
                                        ? deletedAt.Value.ToString("dd.MM.yyyy HH:mm") : "—",
                        DeletedByName = row["DeletedByName"]?.ToString() ?? "—"
                    });
                }

                var total = _allRows.Count;
                TxtTotal.Text  = total == 0
                    ? "корзина пуста"
                    : $"{total} {Plural(total, "запись", "записи", "записей")} в корзине";
                TxtStatus.Text = $"Загружено: {total} записей · Отображается: {_allRows.Count}";

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки корзины:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Фильтрация ─────────────────────────────────────────────────────

        private void ApplyFilter()
        {
            if (RestoreGrid == null || EmptyState == null) return;

            var filtered = _allRows.AsEnumerable();

            // Фильтр по типу
            if (_activeFilter != "All")
                filtered = filtered.Where(r => r.TableName == _activeFilter);

            // Поиск по тексту
            var search = TxtSearch?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(r =>
                    r.RecordName.ToLower().Contains(search)    ||
                    r.ExtraInfo.ToLower().Contains(search)     ||
                    r.GroupInfo.ToLower().Contains(search)     ||
                    r.DeletedByName.ToLower().Contains(search));

            var result = filtered.ToList();
            RestoreGrid.ItemsSource = result;

            bool isEmpty = result.Count == 0;
            EmptyState.Visibility  = isEmpty ? Visibility.Visible  : Visibility.Collapsed;
            RestoreGrid.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;

            TxtStatus.Text = $"Отображается: {result.Count} из {_allRows.Count}";
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => FetchAndDisplay();

        // ── Переключение чипов фильтра ──────────────────────────────────────

        private void ChipFilter_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            _activeFilter = btn.Tag?.ToString() ?? "All";
            UpdateChipStyles();
            ApplyFilter();
        }

        private void UpdateChipStyles()
        {
            var activeBlue = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 120, 212));
            var inactiveGray = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(240, 244, 251));

            foreach (var btn in new[] { BtnAll, BtnStudents, BtnDocuments, BtnParents })
            {
                bool isActive = btn.Tag?.ToString() == _activeFilter;

                // Ищем Border внутри шаблона кнопки и меняем его Background
                if (btn.Template?.FindName("Bdr", btn) is System.Windows.Controls.Border bdr)
                {
                    bdr.Background = isActive ? activeBlue : inactiveGray;

                    // Находим TextBlock внутри Border и меняем цвет текста
                    if (bdr.Child is TextBlock tb)
                        tb.Foreground = isActive
                            ? System.Windows.Media.Brushes.White
                            : new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(61, 74, 96));
                }
            }
        }

        // ── Восстановление записи ───────────────────────────────────────────

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || !(btn.Tag is DeletedRow row)) return;

            var confirm = MessageBox.Show(
                $"Восстановить запись?\n\n" +
                $"Тип: {row.TableNameRu}\n" +
                $"Запись: {row.RecordName}\n" +
                (row.GroupInfo != "—" ? $"Группа / связь: {row.GroupInfo}\n" : "") +
                $"\nЗапись будет возвращена в систему и станет снова доступна.",
                "Подтверждение восстановления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_RestoreRecord", new[]
                {
                    new SqlParameter("@TableName",    row.TableName),
                    new SqlParameter("@RecordId",     row.RecordId),
                    new SqlParameter("@RestoredById", SessionHelper.UserId)
                });

                int restored = 0;
                if (dt != null && dt.Rows.Count > 0)
                    restored = Convert.ToInt32(dt.Rows[0]["RestoredCount"]);

                if (restored > 0)
                {
                    MessageBox.Show(
                        $"✅ Запись «{row.RecordName}» успешно восстановлена.",
                        "Восстановлено", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Убираем из списка без перезагрузки
                    _allRows.Remove(row);
                    ApplyFilter();
                    TxtTotal.Text = $"{_allRows.Count} {Plural(_allRows.Count, "запись", "записи", "записей")} в корзине";
                }
                else
                {
                    MessageBox.Show(
                        "Запись не найдена или уже восстановлена.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при восстановлении:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Вспомогательные методы ─────────────────────────────────────────

        private static string GetTableRu(string tableName)
        {
            switch (tableName)
            {
                case "Students":  return "Студент";
                case "Documents": return "Документ";
                case "Parents":   return "Родитель";
                default:          return tableName ?? "—";
            }
        }

        private static string Plural(int n, string one, string few, string many)
        {
            var mod10  = n % 10;
            var mod100 = n % 100;
            if (mod10 == 1 && mod100 != 11)           return one;
            if (mod10 >= 2 && mod10 <= 4 &&
                (mod100 < 10 || mod100 >= 20))         return few;
            return many;
        }
    }

    // ── Модель строки ──────────────────────────────────────────────────────

    public class DeletedRow
    {
        public string    TableName     { get; set; }
        public string    TableNameRu   { get; set; }
        public int       RecordId      { get; set; }
        public string    RecordName    { get; set; }
        public string    ExtraInfo     { get; set; }
        public string    GroupInfo     { get; set; }
        public DateTime? DeletedAt     { get; set; }
        public string    DeletedAtStr  { get; set; }
        public string    DeletedByName { get; set; }
    }
}
