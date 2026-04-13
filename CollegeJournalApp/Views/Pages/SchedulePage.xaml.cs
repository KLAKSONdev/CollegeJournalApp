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

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var filtered = _all.AsEnumerable();
            if (CmbDay?.SelectedIndex > 0)
                filtered = filtered.Where(r => r.DayNum == CmbDay.SelectedIndex);

            var result = filtered.OrderBy(r => r.DayNum).ThenBy(r => r.LessonNum).ToList();
            SchedGrid.ItemsSource = result;
            TxtTotal.Text = $"— {result.Count} занятий";
        }

        private void CmbDay_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();

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
