using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using ClosedXML.Excel;

namespace CollegeJournalApp.Views.Pages
{
    public partial class SchedulePage : Page
    {
        private List<SchedRow> _all = new List<SchedRow>();
        private static readonly string[] Days = { "", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };

        public SchedulePage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetGroupSchedule", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });

                _all.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    int dow = r["DayOfWeek"] != DBNull.Value ? Convert.ToInt32(r["DayOfWeek"]) : 0;
                    var st = r["StartTime"]?.ToString() ?? "";
                    var et = r["EndTime"]?.ToString()   ?? "";

                    _all.Add(new SchedRow
                    {
                        DayNum    = dow,
                        DayName   = dow > 0 && dow < Days.Length ? Days[dow] : "—",
                        LessonNum = r["LessonNumber"]?.ToString() ?? "—",
                        Time      = $"{st} – {et}",
                        Subject   = r["SubjectName"]?.ToString()  ?? "—",
                        Classroom = r["Classroom"]?.ToString()    ?? "—",
                        Teacher   = r["TeacherName"]?.ToString()  ?? "—",
                        WeekType  = r["WeekType"]?.ToString()     ?? "Обе"
                    });
                }

                RenderScheduleAsDiary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderScheduleAsDiary()
        {
            if (DaysPanel == null) return;

            DaysPanel.Children.Clear();

            var filtered = _all.AsEnumerable();
            if (CmbDay?.SelectedIndex > 0)
                filtered = filtered.Where(r => r.DayNum == CmbDay.SelectedIndex);

            var grouped = filtered.GroupBy(r => r.DayNum).OrderBy(g => g.Key);

            foreach (var dayGroup in grouped)
            {
                int dayNum = dayGroup.Key;
                if (dayNum <= 0 || dayNum >= Days.Length) continue;

                string dayName = Days[dayNum];

                // Карточка дня
                var dayCard = new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224)),
                    BorderThickness = new System.Windows.Thickness(1),
                    CornerRadius = new System.Windows.CornerRadius(8),
                    Margin = new System.Windows.Thickness(0, 0, 0, 16),
                    Padding = new System.Windows.Thickness(16)
                };

                var stack = new StackPanel { Orientation = Orientation.Vertical };

                // Заголовок дня
                var header = new TextBlock
                {
                    Text = dayName,
                    FontSize = 16,
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 31, 31)),
                    Margin = new System.Windows.Thickness(0, 0, 0, 12)
                };
                stack.Children.Add(header);

                // Таблица пар
                var grid = new System.Windows.Controls.DataGrid
                {
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    GridLinesVisibility = System.Windows.Controls.DataGridGridLinesVisibility.Horizontal,
                    HorizontalGridLinesBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 242, 241)),
                    BorderThickness = new System.Windows.Thickness(0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                    RowBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                    AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250)),
                    HeadersVisibility = System.Windows.Controls.DataGridHeadersVisibility.Column,
                    CanUserResizeRows = false,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 12,
                    RowHeight = 40,
                    ItemsSource = dayGroup.OrderBy(r => r.LessonNum).ToList()
                };

                // Стили заголовков
                var columnHeaderStyle = new System.Windows.Style(typeof(System.Windows.Controls.DataGridColumnHeader));
                columnHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BackgroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 242, 241))));
                columnHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 94, 92))));
                columnHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.FontSizeProperty, 11.0));
                columnHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.FontWeightProperty, System.Windows.FontWeights.SemiBold));
                columnHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.PaddingProperty, new System.Windows.Thickness(12, 8)));
                columnHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BorderBrushProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 224))));
                columnHeaderStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BorderThicknessProperty, new System.Windows.Thickness(0, 0, 0, 1)));
                grid.ColumnHeaderStyle = columnHeaderStyle;

                // Стили ячеек
                var cellStyle = new System.Windows.Style(typeof(System.Windows.Controls.DataGridCell));
                cellStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BorderThicknessProperty, new System.Windows.Thickness(0)));
                cellStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.PaddingProperty, new System.Windows.Thickness(12, 0)));
                cellStyle.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.VerticalContentAlignmentProperty, System.Windows.VerticalAlignment.Center));
                var trigger = new System.Windows.Trigger(typeof(System.Windows.Controls.DataGridCell), System.Windows.Controls.DataGridCell.IsSelectedProperty, true);
                trigger.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BackgroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 246, 252))));
                trigger.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.ForegroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 31, 31))));
                cellStyle.Triggers.Add(trigger);
                grid.CellStyle = cellStyle;

                // Колонки: № пары, Предмет, Аудитория
                grid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "№",
                    Binding = new System.Windows.Data.Binding(nameof(SchedRow.LessonNum)),
                    Width = 60
                });
                grid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Предмет",
                    Binding = new System.Windows.Data.Binding(nameof(SchedRow.Subject)),
                    Width = new System.Windows.Data.DataGridLength(1, System.Windows.Data.DataGridLengthUnitType.Star)
                });
                grid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Аудитория",
                    Binding = new System.Windows.Data.Binding(nameof(SchedRow.Classroom)),
                    Width = 100
                });

                stack.Children.Add(grid);
                dayCard.Child = stack;
                DaysPanel.Children.Add(dayCard);
            }

            int totalCount = filtered.Count();
            TxtTotal.Text = $"— {totalCount} занятий";
        }

        private void CmbDay_Changed(object sender, SelectionChangedEventArgs e) => RenderScheduleAsDiary();

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Файлы Excel|*.xlsx;*.xls|Все файлы|*.*",
                Title = "Выберите файл с расписанием"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                ImportScheduleFromExcel(dlg.FileName);
                MessageBox.Show("Расписание успешно загружено!", "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData(); // Обновить данные из БД после импорта
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке расписания:\n" + ex.Message, "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportScheduleFromExcel(string filePath)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var row = 2; // Пропускаем заголовок

                while (!worksheet.Cell(row, 1).IsEmpty())
                {
                    var dayName = worksheet.Cell(row, 1).GetValue<string>();
                    var lessonNum = worksheet.Cell(row, 2).GetValue<string>();
                    var time = worksheet.Cell(row, 3).GetValue<string>();
                    var subject = worksheet.Cell(row, 4).GetValue<string>();
                    var classroom = worksheet.Cell(row, 5).GetValue<string>();
                    var teacher = worksheet.Cell(row, 6).GetValue<string>();
                    var weekType = worksheet.Cell(row, 7).GetValue<string>();

                    // Определяем день недели по названию
                    int dayNum = Array.IndexOf(Days, dayName);
                    if (dayNum < 0) dayNum = 0;

                    // Парсим время для получения StartTime и EndTime
                    string startTime = "", endTime = "";
                    if (!string.IsNullOrEmpty(time) && time.Contains("–"))
                    {
                        var parts = time.Split('–');
                        if (parts.Length == 2)
                        {
                            startTime = parts[0].Trim();
                            endTime = parts[1].Trim();
                        }
                    }

                    // Вставляем данные в базу через хранимую процедуру
                    var parameters = new[]
                    {
                        new SqlParameter("@DayOfWeek", dayNum),
                        new SqlParameter("@LessonNumber", string.IsNullOrEmpty(lessonNum) || lessonNum == "—" ? DBNull.Value : (object)lessonNum),
                        new SqlParameter("@StartTime", string.IsNullOrEmpty(startTime) ? DBNull.Value : (object)startTime),
                        new SqlParameter("@EndTime", string.IsNullOrEmpty(endTime) ? DBNull.Value : (object)endTime),
                        new SqlParameter("@SubjectName", string.IsNullOrEmpty(subject) || subject == "—" ? DBNull.Value : (object)subject),
                        new SqlParameter("@Classroom", string.IsNullOrEmpty(classroom) || classroom == "—" ? DBNull.Value : (object)classroom),
                        new SqlParameter("@TeacherName", string.IsNullOrEmpty(teacher) || teacher == "—" ? DBNull.Value : (object)teacher),
                        new SqlParameter("@WeekType", string.IsNullOrEmpty(weekType) ? DBNull.Value : (object)weekType),
                        new SqlParameter("@UserId", SessionHelper.UserId),
                        new SqlParameter("@RoleName", SessionHelper.RoleName)
                    };

                    DatabaseHelper.ExecuteNonQuery("sp_ImportScheduleItem", parameters);
                    row++;
                }
            }
        }
    }

    public class SchedRow
    {
        public int    DayNum    { get; set; }
        public string DayName   { get; set; }
        public string LessonNum { get; set; }
        public string Time      { get; set; }
        public string Subject   { get; set; }
        public string Classroom { get; set; }
        public string Teacher   { get; set; }
        public string WeekType  { get; set; }
    }
}
