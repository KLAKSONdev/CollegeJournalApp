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
