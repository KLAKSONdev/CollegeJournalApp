using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosedXML.Excel;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CollegeJournalApp.Views.Pages
{
    public partial class StudentsPage : Page
    {
        private List<StudentRow> _allStudents = new List<StudentRow>();
        private bool _loaded = false;

        private static readonly string[] _avatarColors = {
            "#0078D4","#107C10","#CA5010","#8764B8","#038387","#C43E1C","#004578","#486860"
        };

        public StudentsPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadStudents();
        }

        private void LoadStudents()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentsByRole", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });

                _allStudents.Clear();
                int i = 1;
                foreach (DataRow row in dt.Rows)
                {
                    var dorm = row["DormitoryName"] != DBNull.Value
                        ? row["DormitoryName"] + (row["RoomNumber"] != DBNull.Value ? ", к." + row["RoomNumber"] : "")
                        : "—";
                    bool isHead  = row["IsHeadman"] != DBNull.Value && Convert.ToBoolean(row["IsHeadman"]);
                    var fullName = row["FullName"]?.ToString() ?? "";

                    // Фото
                    ImageSource photo = null;
                    try
                    {
                        if (row.Table.Columns.Contains("PhotoData") &&
                            row["PhotoData"] != DBNull.Value &&
                            row["PhotoData"] is byte[] bytes && bytes.Length > 0)
                        {
                            // НЕ закрываем MemoryStream — BitmapImage его держит
                            var ms = new MemoryStream(bytes);
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.StreamSource     = ms;
                            bmp.CacheOption      = BitmapCacheOption.OnLoad;
                            bmp.DecodePixelWidth = 72;
                            bmp.EndInit();
                            bmp.Freeze();
                            photo = bmp;
                        }
                    }
                    catch { }

                    var initials    = GetInitials(fullName);
                    var avatarColor = _avatarColors[Math.Abs(fullName.GetHashCode()) % _avatarColors.Length];

                    _allStudents.Add(new StudentRow
                    {
                        RowNum          = i++,
                        StudentId       = Convert.ToInt32(row["StudentId"]),
                        FullName        = fullName,
                        GroupName       = row["GroupName"]?.ToString()   ?? "—",
                        StudentCode     = row["StudentCode"]?.ToString() ?? "—",
                        BirthDate       = row["BirthDate"] != DBNull.Value ? Convert.ToDateTime(row["BirthDate"]).ToString("dd.MM.yyyy") : "—",
                        Gender          = row["Gender"]?.ToString()      ?? "—",
                        StudyBasis      = row["StudyBasis"]?.ToString()  ?? "—",
                        Dormitory       = dorm.ToString(),
                        Phone           = row["Phone"]?.ToString()       ?? "—",
                        IsHeadman       = isHead,
                        Status          = isHead ? "Староста" : "Студент",
                        Photo           = photo,
                        Initials        = initials,
                        AvatarColor     = avatarColor,
                        HasPhoto        = photo != null
                    });
                }

                TxtStudentCount.Text = $"— {_allStudents.Count} чел.";

                if (SessionHelper.IsStudent)
                {
                    TxtHint.Visibility = Visibility.Collapsed;
                    CmbDorm.Visibility = Visibility.Collapsed;
                    StudentsGrid.Columns[7].Visibility = Visibility.Collapsed;
                    StudentsGrid.Columns[8].Visibility = Visibility.Collapsed;
                }

                if (SessionHelper.IsAdmin)
                {
                    CmbGroup.Visibility = Visibility.Visible;
                    CmbGroup.Items.Clear();
                    CmbGroup.Items.Add(new ComboBoxItem { Content = "Все группы" });
                    foreach (var g in _allStudents.Select(s => s.GroupName).Distinct().OrderBy(g => g))
                        CmbGroup.Items.Add(new ComboBoxItem { Content = g });
                    CmbGroup.SelectedIndex = 0;
                }

                _loaded = true;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки студентов:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string GetInitials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "?";
            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : fullName.Substring(0, 1).ToUpper();
        }

        private void ApplyFilters()
        {
            if (!_loaded) return;
            var filtered = _allStudents.AsEnumerable();

            var search = TxtSearch?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(s => s.FullName?.ToLower().Contains(search) == true);

            if (CmbGroup?.Visibility == Visibility.Visible && CmbGroup.SelectedIndex > 0)
            {
                var grp = (CmbGroup.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(grp)) filtered = filtered.Where(s => s.GroupName == grp);
            }

            if (CmbBasis?.SelectedIndex == 1) filtered = filtered.Where(s => s.StudyBasis == "Бюджет");
            else if (CmbBasis?.SelectedIndex == 2) filtered = filtered.Where(s => s.StudyBasis == "Контракт");

            if (CmbGender?.SelectedIndex == 1) filtered = filtered.Where(s => s.Gender == "Мужской");
            else if (CmbGender?.SelectedIndex == 2) filtered = filtered.Where(s => s.Gender == "Женский");

            if (CmbDorm?.Visibility == Visibility.Visible)
            {
                if (CmbDorm.SelectedIndex == 1) filtered = filtered.Where(s => s.Dormitory != "—");
                else if (CmbDorm.SelectedIndex == 2) filtered = filtered.Where(s => s.Dormitory == "—");
            }

            var result = filtered.ToList();
            for (int i = 0; i < result.Count; i++) result[i].RowNum = i + 1;
            StudentsGrid.ItemsSource = result;
            TxtStudentCount.Text = $"— {result.Count} чел.";
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilters();

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";
            if (CmbGroup.Visibility == Visibility.Visible) CmbGroup.SelectedIndex = 0;
            CmbBasis.SelectedIndex  = 0;
            CmbGender.SelectedIndex = 0;
            if (CmbDorm.Visibility == Visibility.Visible) CmbDorm.SelectedIndex = 0;
        }

        private void StudentsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SessionHelper.IsStudent) return;
            if (StudentsGrid.SelectedItem is StudentRow row)
            {
                var card = new StudentCardWindow(row.StudentId, row.FullName);
                card.Owner = Window.GetWindow(this); // безопаснее чем Application.Current.MainWindow
                card.ShowDialog();
                LoadStudents(); // обновляем фото если изменилось
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var source = StudentsGrid.ItemsSource as List<StudentRow>;
            if (source == null || source.Count == 0)
            { MessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information); return; }

            var dlg = new SaveFileDialog { Title="Сохранить список студентов", Filter="Excel файл|*.xlsx",
                FileName=$"Студенты_{DateTime.Now:yyyy-MM-dd_HH-mm}" };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Студенты");
                    ws.Style.Font.FontName = "Arial"; ws.Style.Font.FontSize = 10;
                    ws.Cell(1,1).Value = "Список студентов";
                    ws.Cell(1,1).Style.Font.Bold = true; ws.Cell(1,1).Style.Font.FontSize = 14;
                    ws.Cell(2,1).Value = $"Дата выгрузки: {DateTime.Now:dd.MM.yyyy HH:mm}";
                    ws.Cell(2,1).Style.Font.FontColor = XLColor.Gray;

                    var filterDesc = BuildFilterDescription();
                    int headerRow = 4;
                    if (!string.IsNullOrEmpty(filterDesc))
                    {
                        ws.Cell(3,1).Value = $"Фильтры: {filterDesc}";
                        ws.Cell(3,1).Style.Font.FontColor = XLColor.FromArgb(0,120,212);
                        ws.Cell(3,1).Style.Font.Italic = true;
                        headerRow = 5;
                    }

                    string[] headers = { "№","ФИО","Группа","Зач. книжка","Дата рожд.","Пол","Основание","Общежитие","Телефон","Статус" };
                    for (int c = 0; c < headers.Length; c++)
                    {
                        var cell = ws.Cell(headerRow, c+1);
                        cell.Value = headers[c]; cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0,120,212);
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    for (int i = 0; i < source.Count; i++)
                    {
                        var row = source[i]; int dr = headerRow + 1 + i;
                        ws.Cell(dr,1).Value  = i+1;            ws.Cell(dr,2).Value  = row.FullName    ?? "";
                        ws.Cell(dr,3).Value  = row.GroupName   ?? ""; ws.Cell(dr,4).Value  = row.StudentCode ?? "";
                        ws.Cell(dr,5).Value  = row.BirthDate   ?? ""; ws.Cell(dr,6).Value  = row.Gender      ?? "";
                        ws.Cell(dr,7).Value  = row.StudyBasis  ?? ""; ws.Cell(dr,8).Value  = row.Dormitory   ?? "";
                        ws.Cell(dr,9).Value  = row.Phone       ?? ""; ws.Cell(dr,10).Value = row.Status      ?? "";
                        if (i%2==1) ws.Range(dr,1,dr,10).Style.Fill.BackgroundColor = XLColor.FromArgb(245,245,245);
                        if (row.IsHeadman) ws.Cell(dr,10).Style.Font.FontColor = XLColor.FromArgb(0,120,212);
                    }

                    ws.Column(1).Width=5; ws.Column(2).Width=30; ws.Column(3).Width=12; ws.Column(4).Width=14;
                    ws.Column(5).Width=13; ws.Column(6).Width=10; ws.Column(7).Width=12; ws.Column(8).Width=22;
                    ws.Column(9).Width=16; ws.Column(10).Width=12;
                    ws.SheetView.FreezeRows(headerRow);
                    wb.SaveAs(dlg.FileName);
                }

                if (MessageBox.Show($"Файл сохранён.\n\nОткрыть файл?", "Экспорт завершён",
                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildFilterDescription()
        {
            var parts = new List<string>();
            var search = TxtSearch?.Text?.Trim();
            if (!string.IsNullOrEmpty(search)) parts.Add("Поиск: '" + search + "'");
            if (CmbGroup?.Visibility == Visibility.Visible && CmbGroup.SelectedIndex > 0)
            {
                var grp = (CmbGroup.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(grp)) parts.Add($"Группа: {grp}");
            }
            if (CmbBasis?.SelectedIndex == 1) parts.Add("Бюджет");
            else if (CmbBasis?.SelectedIndex == 2) parts.Add("Контракт");
            if (CmbGender?.SelectedIndex == 1) parts.Add("Мужской");
            else if (CmbGender?.SelectedIndex == 2) parts.Add("Женский");
            if (CmbDorm?.Visibility == Visibility.Visible)
            {
                if (CmbDorm.SelectedIndex == 1) parts.Add("В общежитии");
                else if (CmbDorm.SelectedIndex == 2) parts.Add("Без общежития");
            }
            return string.Join(", ", parts);
        }
    }

    public class StudentRow
    {
        public int         RowNum      { get; set; }
        public int         StudentId   { get; set; }
        public string      FullName    { get; set; }
        public string      GroupName   { get; set; }
        public string      StudentCode { get; set; }
        public string      BirthDate   { get; set; }
        public string      Gender      { get; set; }
        public string      StudyBasis  { get; set; }
        public string      Dormitory   { get; set; }
        public string      Phone       { get; set; }
        public bool        IsHeadman   { get; set; }
        public string      Status      { get; set; }
        public ImageSource Photo       { get; set; }
        public string      Initials    { get; set; }
        public string      AvatarColor { get; set; }
        public bool        HasPhoto    { get; set; }
    }
}
