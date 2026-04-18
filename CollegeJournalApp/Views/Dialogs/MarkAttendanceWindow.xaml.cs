using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    // Данные об одном занятии в ComboBox
    internal class LessonItem
    {
        public int    ScheduleId   { get; set; }
        public int    GroupId      { get; set; }
        public string SubjectName  { get; set; }
        public string GroupName    { get; set; }
        public string StartTime    { get; set; }
        public int    LessonNumber { get; set; }
        public string Classroom    { get; set; }
        public string DisplayName  =>
            $"Пара {LessonNumber}  {StartTime}  {SubjectName} — {GroupName} ({Classroom})";
    }

    // Статус одного студента (изменяется кнопками)
    internal class StudentMarkItem
    {
        public int    StudentId    { get; set; }
        public string StudentName  { get; set; }
        public string Status       { get; set; } = "Присутствовал";
        public string Reason       { get; set; } = "";
        public int?   AttendanceId { get; set; }

        // Ссылки на UI-элементы строки для обновления внешнего вида
        public List<Button> StatusButtons { get; set; } = new List<Button>();
        public TextBox      ReasonBox     { get; set; }
        public StackPanel   ReasonPanel   { get; set; }
    }

    public partial class MarkAttendanceWindow : Window
    {
        private readonly int    _userId;
        private readonly string _roleName;
        private readonly List<StudentMarkItem> _students = new List<StudentMarkItem>();

        // Текущее выбранное занятие
        private LessonItem _currentLesson;

        public MarkAttendanceWindow(int userId, string roleName)
        {
            InitializeComponent();
            _userId   = userId;
            _roleName = roleName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DtPicker.SelectedDate = DateTime.Today;
            // Загрузка занятий запустится через событие SelectedDateChanged
        }

        // ── Выбор даты → загрузка занятий ──────────────────────────────────

        private void DtPicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (DtPicker.SelectedDate == null) return;
            LoadLessons(DtPicker.SelectedDate.Value);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (DtPicker.SelectedDate != null)
                LoadLessons(DtPicker.SelectedDate.Value);
        }

        private void LoadLessons(DateTime date)
        {
            CmbLesson.ItemsSource   = null;
            CmbLesson.Items.Clear();
            ClearStudentList();

            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetLessonsForMarking", new[]
                {
                    new SqlParameter("@UserId",   _userId),
                    new SqlParameter("@RoleName", _roleName),
                    new SqlParameter("@Date",     date.Date)
                });

                var lessons = new List<LessonItem>();
                foreach (DataRow r in dt.Rows)
                    lessons.Add(new LessonItem
                    {
                        ScheduleId   = Convert.ToInt32(r["ScheduleId"]),
                        GroupId      = Convert.ToInt32(r["GroupId"]),
                        SubjectName  = r["SubjectName"]?.ToString() ?? "—",
                        GroupName    = r["GroupName"]?.ToString()   ?? "—",
                        StartTime    = r["StartTime"]?.ToString()   ?? "",
                        LessonNumber = Convert.ToInt32(r["LessonNumber"]),
                        Classroom    = r["Classroom"]?.ToString()   ?? "—"
                    });

                CmbLesson.ItemsSource = lessons;

                if (lessons.Count > 0)
                    CmbLesson.SelectedIndex = 0;
                else
                {
                    TxtEmpty.Text = $"Нет занятий на {date:dd.MM.yyyy} (выходной или нет расписания)";
                    EmptyState.Visibility = Visibility.Visible;
                    Scroll.Visibility     = Visibility.Collapsed;
                    InfoStrip.Visibility  = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки занятий:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Выбор занятия → загрузка студентов ─────────────────────────────

        private void CmbLesson_Changed(object sender, SelectionChangedEventArgs e)
        {
            _currentLesson = CmbLesson.SelectedItem as LessonItem;
            if (_currentLesson == null || DtPicker.SelectedDate == null) return;
            LoadStudents(_currentLesson, DtPicker.SelectedDate.Value);
        }

        private void LoadStudents(LessonItem lesson, DateTime date)
        {
            ClearStudentList();

            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentsForMarking", new[]
                {
                    new SqlParameter("@ScheduleId", lesson.ScheduleId),
                    new SqlParameter("@LessonDate", date.Date)
                });

                if (dt.Rows.Count == 0)
                {
                    TxtEmpty.Text = "В группе нет студентов.";
                    EmptyState.Visibility = Visibility.Visible;
                    Scroll.Visibility     = Visibility.Collapsed;
                    InfoStrip.Visibility  = Visibility.Collapsed;
                    return;
                }

                foreach (DataRow r in dt.Rows)
                {
                    var item = new StudentMarkItem
                    {
                        StudentId    = Convert.ToInt32(r["StudentId"]),
                        StudentName  = r["StudentName"]?.ToString()    ?? "—",
                        Status       = r["CurrentStatus"]?.ToString()  ?? "Присутствовал",
                        Reason       = r["CurrentReason"]?.ToString()  ?? "",
                        AttendanceId = r["AttendanceId"] != DBNull.Value
                                       ? (int?)Convert.ToInt32(r["AttendanceId"]) : null
                    };
                    _students.Add(item);
                    BuildStudentRow(item);
                }

                // Информационная полоса
                TxtInfoSubject.Text = lesson.SubjectName;
                TxtInfoGroup.Text   = $"/ {lesson.GroupName}";
                TxtInfoCount.Text   = $"{_students.Count} студентов";
                InfoStrip.Visibility  = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
                Scroll.Visibility     = Visibility.Visible;

                BtnMarkAll.IsEnabled = true;
                BtnSave.IsEnabled    = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки студентов:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Построение строки студента ──────────────────────────────────────

        private void BuildStudentRow(StudentMarkItem item)
        {
            var rowBorder = new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(20, 10, 20, 10),
                Background      = Brushes.White
            };
            rowBorder.MouseEnter += (s, e) =>
                rowBorder.Background = new SolidColorBrush(Color.FromRgb(250, 250, 252));
            rowBorder.MouseLeave += (s, e) =>
                rowBorder.Background = Brushes.White;

            var outer = new StackPanel();

            // Имя студента
            outer.Children.Add(new TextBlock
            {
                Text       = item.StudentName,
                FontSize   = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(31, 31, 31)),
                Margin     = new Thickness(0, 0, 0, 7)
            });

            // Кнопки статуса
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var statuses = new[]
            {
                ("Присутствовал",     "#107C10"),
                ("Отсутствовал",      "#D13438"),
                ("Опоздал",           "#CA5010"),
                ("Уважит. причина",   "#0078D4")
            };

            // Поле причины (создаём заранее, используем в обработчиках)
            var reasonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 7, 0, 0),
                Visibility  = item.Status == "Присутствовал" ? Visibility.Collapsed : Visibility.Visible
            };
            reasonPanel.Children.Add(new TextBlock
            {
                Text                = "Причина:",
                FontSize            = 12,
                Foreground          = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                VerticalAlignment   = VerticalAlignment.Center,
                Margin              = new Thickness(0, 0, 8, 0)
            });
            var reasonBox = new TextBox
            {
                Width       = 270,
                Height      = 28,
                Text        = item.Reason,
                FontSize    = 12,
                Padding     = new Thickness(8, 4, 8, 4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            reasonBox.TextChanged += (s, e) => item.Reason = reasonBox.Text;
            reasonPanel.Children.Add(reasonBox);

            item.ReasonBox   = reasonBox;
            item.ReasonPanel = reasonPanel;

            // Создаём кнопки
            foreach (var (statusLabel, colorHex) in statuses)
            {
                var isSelected = item.Status == statusLabel
                    || (statusLabel == "Уважит. причина" && item.Status == "Уважительная причина");
                var btn = CreateStatusButton(statusLabel, colorHex, isSelected);
                btn.Tag = statusLabel == "Уважит. причина" ? "Уважительная причина" : statusLabel;

                btn.Click += (s, e) =>
                {
                    var newStatus = (string)((Button)s).Tag;
                    item.Status = newStatus;

                    // Обновляем внешний вид всех кнопок строки
                    foreach (var b in item.StatusButtons)
                    {
                        var bStatus = (string)b.Tag;
                        var bColor  = bStatus == "Присутствовал" ? "#107C10"
                                    : bStatus == "Отсутствовал"  ? "#D13438"
                                    : bStatus == "Опоздал"        ? "#CA5010"
                                    : "#0078D4";
                        if (bStatus == newStatus)
                        {
                            b.Background = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString(bColor));
                            b.Foreground = Brushes.White;
                            b.BorderBrush = Brushes.Transparent;
                        }
                        else
                        {
                            b.Background = Brushes.White;
                            b.Foreground = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString(bColor));
                            b.BorderBrush = new SolidColorBrush(
                                (Color)ColorConverter.ConvertFromString(bColor));
                        }
                    }

                    item.ReasonPanel.Visibility =
                        newStatus == "Присутствовал" ? Visibility.Collapsed : Visibility.Visible;
                    if (newStatus == "Присутствовал") item.Reason = "";
                };

                item.StatusButtons.Add(btn);
                btnPanel.Children.Add(btn);
            }

            outer.Children.Add(btnPanel);
            outer.Children.Add(reasonPanel);

            rowBorder.Child = outer;
            StudentsPanel.Children.Add(rowBorder);
        }

        private Button CreateStatusButton(string label, string colorHex, bool selected)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var btn = new Button
            {
                Content         = label,
                Height          = 28,
                Padding         = new Thickness(10, 0, 10, 0),
                Margin          = new Thickness(0, 0, 6, 0),
                FontSize        = 11,
                Cursor          = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(1)
            };

            if (selected)
            {
                btn.Background  = new SolidColorBrush(color);
                btn.Foreground  = Brushes.White;
                btn.BorderBrush = Brushes.Transparent;
            }
            else
            {
                btn.Background  = Brushes.White;
                btn.Foreground  = new SolidColorBrush(color);
                btn.BorderBrush = new SolidColorBrush(color);
            }

            return btn;
        }

        private void ClearStudentList()
        {
            _students.Clear();
            StudentsPanel.Children.Clear();
            BtnMarkAll.IsEnabled  = false;
            BtnSave.IsEnabled     = false;
        }

        // ── Кнопка «Всех присутствующими» ──────────────────────────────────

        private void BtnMarkAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _students)
            {
                item.Status = "Присутствовал";
                item.Reason = "";

                // Визуально обновляем кнопки
                foreach (var btn in item.StatusButtons)
                {
                    var bStatus = (string)btn.Tag;
                    if (bStatus == "Присутствовал")
                    {
                        btn.Background  = new SolidColorBrush(Color.FromRgb(16, 124, 16));
                        btn.Foreground  = Brushes.White;
                        btn.BorderBrush = Brushes.Transparent;
                    }
                    else
                    {
                        var bColor = bStatus == "Отсутствовал"        ? Color.FromRgb(209, 52, 56)
                                   : bStatus == "Опоздал"              ? Color.FromRgb(202, 80, 16)
                                   : Color.FromRgb(0, 120, 212);
                        btn.Background  = Brushes.White;
                        btn.Foreground  = new SolidColorBrush(bColor);
                        btn.BorderBrush = new SolidColorBrush(bColor);
                    }
                }

                if (item.ReasonPanel != null)
                {
                    item.ReasonPanel.Visibility = Visibility.Collapsed;
                    if (item.ReasonBox != null) item.ReasonBox.Text = "";
                }
            }
        }

        // ── Сохранение ──────────────────────────────────────────────────────

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLesson == null || DtPicker.SelectedDate == null || _students.Count == 0)
                return;

            BtnSave.IsEnabled    = false;
            BtnSave.Content      = "Сохраняю...";

            var date   = DtPicker.SelectedDate.Value.Date;
            var errors = new List<string>();

            foreach (var item in _students)
            {
                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_SaveAttendanceMark", new[]
                    {
                        new SqlParameter("@MarkedById", _userId),
                        new SqlParameter("@StudentId",  item.StudentId),
                        new SqlParameter("@ScheduleId", _currentLesson.ScheduleId),
                        new SqlParameter("@LessonDate", date),
                        new SqlParameter("@Status",     item.Status),
                        new SqlParameter("@Reason",     (object)item.Reason ?? DBNull.Value)
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.StudentName}: {ex.Message}");
                }
            }

            BtnSave.IsEnabled = true;
            BtnSave.Content   = "💾 Сохранить";

            if (errors.Count > 0)
            {
                MessageBox.Show("Ошибки при сохранении:\n" + string.Join("\n", errors),
                    "Частичная ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
