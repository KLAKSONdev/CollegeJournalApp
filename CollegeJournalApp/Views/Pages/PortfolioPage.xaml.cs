using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views.Dialogs;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class PortfolioPage : Page
    {
        private List<StudentListItem>  _allStudents  = new List<StudentListItem>();
        private StudentListItem        _selectedStudent;
        private List<AchievementItem>  _achievements = new List<AchievementItem>();
        private StackPanel             _currentAboutSection;

        public PortfolioPage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => LoadGroups();
        }

        // ── Левая панель: группы и студенты ────────────────────────────────

        private void LoadGroups()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAllGroups");

                CmbGroup.Items.Clear();

                if (SessionHelper.IsAdmin)
                {
                    var allItem = new ComboBoxItem { Content = "— Все группы —" };
                    allItem.Tag = (int?)null;
                    CmbGroup.Items.Add(allItem);
                }

                ComboBoxItem defaultItem = null;

                foreach (DataRow row in dt.Rows)
                {
                    int  gid   = Convert.ToInt32(row["GroupId"]);
                    var  name  = row["GroupName"]?.ToString() ?? "";
                    int? curId = row["CuratorId"] != DBNull.Value
                        ? (int?)Convert.ToInt32(row["CuratorId"])
                        : null;

                    var item = new ComboBoxItem { Content = name };
                    item.Tag = (int?)gid;
                    CmbGroup.Items.Add(item);

                    if (SessionHelper.IsCurator && curId == SessionHelper.UserId)
                        defaultItem = item;
                }

                // Куратор видит только свою группу
                if (SessionHelper.IsCurator)
                {
                    CmbGroup.Items.Clear();
                    if (defaultItem != null)
                    {
                        CmbGroup.Items.Add(defaultItem);
                    }
                    else
                    {
                        var fallback = new ComboBoxItem { Content = "— Все группы —" };
                        fallback.Tag = (int?)null;
                        CmbGroup.Items.Add(fallback);
                    }
                }

                if (CmbGroup.Items.Count > 0)
                    CmbGroup.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки групп: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbGroup == null) return;
            if (CmbGroup.SelectedItem is ComboBoxItem item)
            {
                var gid = item.Tag as int?;
                LoadStudents(gid);
            }
        }

        private void LoadStudents(int? groupId)
        {
            try
            {
                var param = groupId.HasValue
                    ? new SqlParameter("@GroupId", groupId.Value)
                    : new SqlParameter("@GroupId", DBNull.Value);

                var dt = DatabaseHelper.ExecuteProcedure("sp_GetPortfolioStudents", new[] { param });

                _allStudents.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    var fullName  = row["FullName"]?.ToString()  ?? "";
                    var lastName  = row["LastName"]?.ToString()  ?? "";
                    var firstName = row["FirstName"]?.ToString() ?? "";

                    var shortName = string.IsNullOrEmpty(firstName)
                        ? lastName
                        : lastName + " " + firstName[0] + ".";

                    var initial = lastName.Length > 0 ? lastName[0].ToString() : "?";

                    _allStudents.Add(new StudentListItem
                    {
                        StudentId        = Convert.ToInt32(row["StudentId"]),
                        FullName         = fullName,
                        ShortName        = shortName,
                        Initial          = initial,
                        GroupName        = row["GroupName"]?.ToString() ?? "",
                        GroupId          = Convert.ToInt32(row["GroupId"]),
                        IsHeadman        = row["IsHeadman"] != DBNull.Value && Convert.ToBoolean(row["IsHeadman"]),
                        StudentCode      = row["StudentCode"]?.ToString() ?? "",
                        AchievementCount = Convert.ToInt32(row["AchievementCount"])
                    });
                }

                ApplyStudentFilter();

                TxtPageSubtitle.Text = _allStudents.Count > 0
                    ? "Студентов: " + _allStudents.Count
                    : "Нет студентов";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки студентов: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtStudentSearch_Changed(object sender, TextChangedEventArgs e)
        {
            if (TxtStudentSearch == null) return;
            ApplyStudentFilter();
        }

        private void ApplyStudentFilter()
        {
            var search = TxtStudentSearch?.Text?.Trim() ?? "";

            List<StudentListItem> filtered;
            if (string.IsNullOrEmpty(search))
            {
                filtered = _allStudents;
            }
            else
            {
                filtered = _allStudents.Where(s =>
                    s.FullName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    s.GroupName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }

            StudentList.ItemsSource = filtered;
        }

        private void StudentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StudentList == null) return;
            var s = StudentList.SelectedItem as StudentListItem;
            if (s == null) return;
            _selectedStudent = s;
            ShowPortfolioContent(s);
        }

        // ── Правая панель: шапка + вкладки ─────────────────────────────────

        private void ShowPortfolioContent(StudentListItem s)
        {
            EmptyState.Visibility       = Visibility.Collapsed;
            PortfolioContent.Visibility = Visibility.Visible;

            TxtAvatarBig.Text      = s.Initial;
            TxtStudentFullName.Text = s.FullName;
            BdrHeadman.Visibility  = s.IsHeadman ? Visibility.Visible : Visibility.Collapsed;
            TxtStudentGroup.Text   = s.GroupName;
            TxtStudentCode.Text    = !string.IsNullOrEmpty(s.StudentCode) ? "№ " + s.StudentCode : "";
            TxtSummaryAchievements.Text = s.AchievementCount.ToString();

            LoadAbout(s.StudentId);
            LoadAchievements(s.StudentId);
            LoadGrades(s.StudentId);
            LoadAttendance(s.StudentId);
            LoadCharacteristic(s.StudentId);

            bool canEditChar = SessionHelper.IsAdmin || SessionHelper.IsCurator;
            TxtCharacteristic.IsReadOnly    = !canEditChar;
            BtnSaveCharacteristic.IsEnabled = canEditChar;
            BtnSaveCharacteristic.Opacity   = canEditChar ? 1.0 : 0.4;

            bool canEditAch = SessionHelper.IsAdmin || SessionHelper.IsCurator;
            BtnAddAchievement.IsEnabled = canEditAch;
            BtnAddAchievement.Opacity   = canEditAch ? 1.0 : 0.4;
        }

        // ── Вкладка 1: О студенте ──────────────────────────────────────────

        private void LoadAbout(int studentId)
        {
            PanelAbout.Children.Clear();
            _currentAboutSection = null;
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentDetails",
                    new[] { new SqlParameter("@StudentId", studentId) });
                if (dt.Rows.Count == 0) return;
                var row = dt.Rows[0];

                AddAboutSection("Личные данные");
                AddAboutRow("Пол",             FormatField(row, "Gender"));
                AddAboutRow("Дата рождения",   FormatDate(row, "BirthDate"));
                AddAboutRow("Место рождения",  FormatField(row, "BirthPlace"));
                AddAboutRow("Гражданство",     FormatField(row, "Citizenship"));

                AddAboutSection("Контакты");
                AddAboutRow("Телефон", FormatField(row, "Phone"));
                AddAboutRow("Email",   FormatField(row, "Email"));
                AddAboutRow("Адрес",   FormatField(row, "Address"));

                AddAboutSection("Учёба");
                AddAboutRow("Основа обучения",  FormatField(row, "StudyBasis"));
                AddAboutRow("Дата зачисления",  FormatDate(row, "EnrollmentDate"));
                AddAboutRow("Предыдущая школа", FormatField(row, "PreviousSchool"));

                var dormName = row["DormitoryName"] != DBNull.Value ? row["DormitoryName"].ToString() : "";
                var roomNum  = row["RoomNumber"] != DBNull.Value    ? row["RoomNumber"].ToString()    : "";
                var dormText = string.IsNullOrEmpty(dormName) ? "—"
                    : dormName + (string.IsNullOrEmpty(roomNum) ? "" : ", к." + roomNum);
                AddAboutRow("Общежитие", dormText);

                AddAboutSection("Документы");
                AddAboutRow("СНИЛС",      FormatField(row, "SNILSNumber"));
                var ps = FormatField(row, "PassportSeries");
                var pn = FormatField(row, "PassportNumber");
                AddAboutRow("Паспорт",    (ps == "—" && pn == "—") ? "—" : (ps + " " + pn).Trim());
                AddAboutRow("Выдан",      FormatField(row, "PassportIssuedBy"));
                AddAboutRow("Дата выдачи",FormatDate(row, "PassportIssuedDate"));
            }
            catch (Exception ex)
            {
                PanelAbout.Children.Add(new TextBlock
                {
                    Text      = "Ошибка загрузки: " + ex.Message,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize  = 12
                });
            }
        }

        private void AddAboutSection(string title)
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text       = title,
                FontSize   = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 74)),
                Margin     = new Thickness(0, 0, 0, 8)
            });

            var border = new Border
            {
                Background      = new SolidColorBrush(Colors.White),
                CornerRadius    = new CornerRadius(10),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(227, 230, 238)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 0, 12),
                Padding         = new Thickness(16, 12, 16, 12),
                Child           = sp
            };
            border.Effect = MakeShadow();

            PanelAbout.Children.Add(border);
            _currentAboutSection = sp;
        }

        private void AddAboutRow(string label, string value)
        {
            if (_currentAboutSection == null) return;

            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblTb = new TextBlock
            {
                Text              = label,
                FontSize          = 12,
                Foreground        = new SolidColorBrush(Color.FromRgb(90, 102, 122)),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(lblTb, 0);
            row.Children.Add(lblTb);

            var valTb = new TextBlock
            {
                Text         = string.IsNullOrWhiteSpace(value) ? "—" : value,
                FontSize     = 12,
                Foreground   = new SolidColorBrush(Color.FromRgb(27, 42, 74)),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(valTb, 1);
            row.Children.Add(valTb);

            _currentAboutSection.Children.Add(row);
        }

        // ── Вкладка 2: Достижения ──────────────────────────────────────────

        private void LoadAchievements(int studentId)
        {
            PanelAchievements.Children.Clear();
            _achievements.Clear();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetPortfolioAchievements",
                    new[] { new SqlParameter("@StudentId", studentId) });

                foreach (DataRow row in dt.Rows)
                {
                    var ach = new AchievementItem
                    {
                        AchievementId  = Convert.ToInt32(row["AchievementId"]),
                        Title          = row["Title"]?.ToString()          ?? "",
                        Category       = row["Category"]?.ToString()       ?? "Другое",
                        Level          = row["Level"]?.ToString()          ?? "",
                        Description    = row["Description"]?.ToString()    ?? "",
                        DocumentNumber = row["DocumentNumber"]?.ToString() ?? "",
                        AddedByName    = row["AddedByName"]?.ToString()    ?? "",
                        AchieveDate    = row["AchieveDate"] != DBNull.Value
                            ? (DateTime?)Convert.ToDateTime(row["AchieveDate"])
                            : null
                    };
                    _achievements.Add(ach);
                    PanelAchievements.Children.Add(BuildAchievementCard(ach));
                }

                TxtAchievementCount.Text = _achievements.Count > 0
                    ? "Всего: " + _achievements.Count
                    : "Нет достижений";
                TxtSummaryAchievements.Text = _achievements.Count.ToString();

                if (_achievements.Count == 0)
                {
                    PanelAchievements.Children.Add(MakeEmptyNote("🏆", "У студента пока нет достижений"));
                }
            }
            catch (Exception ex)
            {
                PanelAchievements.Children.Add(new TextBlock
                {
                    Text      = "Ошибка: " + ex.Message,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize  = 12
                });
            }
        }

        private UIElement BuildAchievementCard(AchievementItem ach)
        {
            Color catBg, catFg;
            GetCategoryColors(ach.Category, out catBg, out catFg);

            var border = new Border
            {
                Background      = new SolidColorBrush(Colors.White),
                CornerRadius    = new CornerRadius(10),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(227, 230, 238)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 0, 10),
                Padding         = new Thickness(16, 12, 16, 12)
            };
            border.Effect = MakeShadow();

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Левая часть: заголовок, бейджи, описание, мета
            var left = new StackPanel();

            // Заголовок
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            titleRow.Children.Add(new TextBlock
            {
                Text              = "🏆",
                FontSize          = 14,
                Margin            = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            titleRow.Children.Add(new TextBlock
            {
                Text              = ach.Title,
                FontSize          = 13,
                FontWeight        = FontWeights.SemiBold,
                Foreground        = new SolidColorBrush(Color.FromRgb(27, 42, 74)),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping      = TextWrapping.Wrap
            });
            left.Children.Add(titleRow);

            // Категория + уровень
            var tagRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            if (!string.IsNullOrEmpty(ach.Category))
                tagRow.Children.Add(MakeBadge(ach.Category, catBg, catFg));
            if (!string.IsNullOrEmpty(ach.Level))
                tagRow.Children.Add(MakeBadge(ach.Level,
                    Color.FromRgb(239, 246, 252), Color.FromRgb(0, 99, 177)));
            left.Children.Add(tagRow);

            // Описание
            if (!string.IsNullOrEmpty(ach.Description))
            {
                left.Children.Add(new TextBlock
                {
                    Text         = ach.Description,
                    FontSize     = 11,
                    Foreground   = new SolidColorBrush(Color.FromRgb(90, 102, 122)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin       = new Thickness(0, 0, 0, 4)
                });
            }

            // Мета-строка: дата и номер документа
            var meta = new StackPanel { Orientation = Orientation.Horizontal };
            if (ach.AchieveDate.HasValue)
            {
                meta.Children.Add(new TextBlock
                {
                    Text      = ach.AchieveDate.Value.ToString("dd.MM.yyyy"),
                    FontSize  = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166)),
                    Margin    = new Thickness(0, 0, 12, 0)
                });
            }
            if (!string.IsNullOrEmpty(ach.DocumentNumber))
            {
                meta.Children.Add(new TextBlock
                {
                    Text      = "№ " + ach.DocumentNumber,
                    FontSize  = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166))
                });
            }
            left.Children.Add(meta);

            Grid.SetColumn(left, 0);
            mainGrid.Children.Add(left);

            // Правая часть: кнопки Редактировать / Удалить
            if (SessionHelper.IsAdmin || SessionHelper.IsCurator)
            {
                var btnPanel = new StackPanel
                {
                    Orientation       = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin            = new Thickness(12, 0, 0, 0)
                };

                var btnEdit   = MakeSmallButton("✏️", "#EFF6FC", ach);
                var btnDelete = MakeSmallButton("🗑️", "#FFF4CE", ach);

                btnEdit.Click   += AchEdit_Click;
                btnDelete.Click += AchDelete_Click;

                btnPanel.Children.Add(btnEdit);
                btnPanel.Children.Add(btnDelete);

                Grid.SetColumn(btnPanel, 1);
                mainGrid.Children.Add(btnPanel);
            }

            border.Child = mainGrid;
            return border;
        }

        private Button MakeSmallButton(string emoji, string bgHex, AchievementItem tag)
        {
            var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
            var btn = new Button
            {
                Tag    = tag,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(4, 0, 0, 0),
                Width  = 30,
                Height = 30
            };

            var tmpl   = new ControlTemplate(typeof(Button));
            var bdrFac = new FrameworkElementFactory(typeof(Border));
            bdrFac.SetValue(Border.BackgroundProperty,   new SolidColorBrush(bgColor));
            bdrFac.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            var tbFac = new FrameworkElementFactory(typeof(TextBlock));
            tbFac.SetValue(TextBlock.TextProperty,               emoji);
            tbFac.SetValue(TextBlock.FontSizeProperty,           13.0);
            tbFac.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            tbFac.SetValue(TextBlock.VerticalAlignmentProperty,   VerticalAlignment.Center);
            bdrFac.AppendChild(tbFac);

            tmpl.VisualTree = bdrFac;
            btn.Template    = tmpl;
            return btn;
        }

        private void AchEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null) return;
            var ach = (sender as Button)?.Tag as AchievementItem;
            if (ach == null) return;

            var existing = new AchievementResult
            {
                AchievementId  = ach.AchievementId,
                Title          = ach.Title,
                Category       = ach.Category,
                Level          = ach.Level,
                Description    = ach.Description,
                DocumentNumber = ach.DocumentNumber,
                AchieveDate    = ach.AchieveDate
            };

            var dlg = new AchievementEditDialog(_selectedStudent.FullName, existing)
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true) return;

            var result        = dlg.Result;
            result.AchievementId = ach.AchievementId;

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_UpdateAchievement", new[]
                {
                    new SqlParameter("@AchievementId",  result.AchievementId.Value),
                    new SqlParameter("@Title",          result.Title),
                    new SqlParameter("@Category",       result.Category),
                    new SqlParameter("@Level",          result.Level),
                    new SqlParameter("@Description",    (object)result.Description    ?? DBNull.Value),
                    new SqlParameter("@DocumentNumber", (object)result.DocumentNumber ?? DBNull.Value),
                    new SqlParameter("@AchieveDate",    (object)result.AchieveDate   ?? DBNull.Value),
                    new SqlParameter("@UpdatedById",    SessionHelper.UserId)
                });
                LoadAchievements(_selectedStudent.StudentId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AchDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null) return;
            var ach = (sender as Button)?.Tag as AchievementItem;
            if (ach == null) return;

            var res = MessageBox.Show(
                "Удалить достижение «" + ach.Title + "»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_DeleteAchievement", new[]
                {
                    new SqlParameter("@AchievementId", ach.AchievementId),
                    new SqlParameter("@DeletedById",   SessionHelper.UserId)
                });
                LoadAchievements(_selectedStudent.StudentId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddAchievement_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null) return;

            var dlg = new AchievementEditDialog(_selectedStudent.FullName)
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true) return;

            var result = dlg.Result;
            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_AddAchievement", new[]
                {
                    new SqlParameter("@StudentId",      _selectedStudent.StudentId),
                    new SqlParameter("@Title",          result.Title),
                    new SqlParameter("@Category",       result.Category),
                    new SqlParameter("@Level",          result.Level),
                    new SqlParameter("@Description",    (object)result.Description    ?? DBNull.Value),
                    new SqlParameter("@DocumentNumber", (object)result.DocumentNumber ?? DBNull.Value),
                    new SqlParameter("@AchieveDate",    (object)result.AchieveDate   ?? DBNull.Value),
                    new SqlParameter("@AddedById",      SessionHelper.UserId)
                });
                LoadAchievements(_selectedStudent.StudentId);

                // Обновляем счётчик в боковом списке
                _selectedStudent.AchievementCount = _achievements.Count;
                StudentList.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Вкладка 3: Успеваемость ────────────────────────────────────────

        private void LoadGrades(int studentId)
        {
            PanelGrades.Children.Clear();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetPortfolioGrades",
                    new[] { new SqlParameter("@StudentId", studentId) });

                if (dt.Rows.Count == 0)
                {
                    PanelGrades.Children.Add(MakeEmptyNote("📊", "Оценок пока нет"));
                    TxtSummaryGrade.Text = "—";
                    return;
                }

                double totalWeighted = 0;
                int    totalCount    = 0;

                foreach (DataRow row in dt.Rows)
                {
                    double avg   = Convert.ToDouble(row["AvgGrade"]);
                    int    cnt   = Convert.ToInt32(row["GradeCount"]);
                    string subj  = row["SubjectName"]?.ToString() ?? "";
                    string last  = row["LastGradeDate"] != DBNull.Value
                        ? Convert.ToDateTime(row["LastGradeDate"]).ToString("dd.MM.yy")
                        : "—";

                    totalWeighted += avg * cnt;
                    totalCount    += cnt;

                    PanelGrades.Children.Add(BuildGradeCard(subj, avg, cnt, last));
                }

                double overall = totalCount > 0 ? totalWeighted / totalCount : 0;
                TxtSummaryGrade.Text = overall > 0 ? overall.ToString("F1") : "—";
            }
            catch (Exception ex)
            {
                PanelGrades.Children.Add(new TextBlock
                {
                    Text      = "Ошибка: " + ex.Message,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize  = 12
                });
            }
        }

        private UIElement BuildGradeCard(string subject, double avg, int count, string lastDate)
        {
            Color bgC, fgC;
            GetGradeBadgeColors(avg, out bgC, out fgC);

            var border = new Border
            {
                Background      = new SolidColorBrush(Colors.White),
                CornerRadius    = new CornerRadius(10),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(227, 230, 238)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 0, 8),
                Padding         = new Thickness(16, 12, 16, 12)
            };
            border.Effect = MakeShadow();

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(new TextBlock
            {
                Text       = subject,
                FontSize   = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 74))
            });
            left.Children.Add(new TextBlock
            {
                Text      = count + " оценок · последняя " + lastDate,
                FontSize  = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166)),
                Margin    = new Thickness(0, 3, 0, 0)
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var badge = new Border
            {
                Width               = 48,
                Height              = 48,
                CornerRadius        = new CornerRadius(24),
                Background          = new SolidColorBrush(bgC),
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            badge.Child = new TextBlock
            {
                Text                = avg.ToString("F1"),
                FontSize            = 16,
                FontWeight          = FontWeights.Bold,
                Foreground          = new SolidColorBrush(fgC),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            };
            Grid.SetColumn(badge, 1);
            grid.Children.Add(badge);

            border.Child = grid;
            return border;
        }

        // ── Вкладка 4: Посещаемость ────────────────────────────────────────

        private void LoadAttendance(int studentId)
        {
            PanelAttendance.Children.Clear();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetPortfolioAttendance",
                    new[] { new SqlParameter("@StudentId", studentId) });

                if (dt.Rows.Count == 0)
                {
                    PanelAttendance.Children.Add(MakeEmptyNote("📅", "Данных о посещаемости нет"));
                    TxtSummaryAttendance.Text = "—";
                    return;
                }

                var statusDict = new Dictionary<string, int>();
                int total = 0;
                foreach (DataRow row in dt.Rows)
                {
                    string status = row["Status"]?.ToString() ?? "";
                    int    cnt    = Convert.ToInt32(row["Cnt"]);
                    statusDict[status] = cnt;
                    total += cnt;
                }

                int    present = statusDict.ContainsKey("П") ? statusDict["П"] : 0;
                double pct     = total > 0 ? (double)present / total * 100.0 : 0;
                TxtSummaryAttendance.Text = pct.ToString("F0") + "%";

                var summaryBorder = new Border
                {
                    Background      = new SolidColorBrush(Colors.White),
                    CornerRadius    = new CornerRadius(10),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(227, 230, 238)),
                    BorderThickness = new Thickness(1),
                    Margin          = new Thickness(0, 0, 0, 12),
                    Padding         = new Thickness(16, 14, 16, 14)
                };
                summaryBorder.Effect = MakeShadow();

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text       = "Сводка посещаемости",
                    FontSize   = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 74)),
                    Margin     = new Thickness(0, 0, 0, 12)
                });

                // Прогресс-бар
                Color fillColor;
                if (pct >= 80)      fillColor = Color.FromRgb(16,  124, 16);
                else if (pct >= 60) fillColor = Color.FromRgb(201, 130,  0);
                else                fillColor = Color.FromRgb(196,  62, 28);

                var progressGrid = new Grid { Height = 10, Margin = new Thickness(0, 0, 0, 12) };

                double safePct = Math.Max(0, Math.Min(100, pct));
                progressGrid.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = new GridLength(safePct, GridUnitType.Star) });
                progressGrid.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = new GridLength(100 - safePct, GridUnitType.Star) });

                var fillBdr = new Border
                {
                    CornerRadius = new CornerRadius(5),
                    Background   = new SolidColorBrush(fillColor)
                };
                Grid.SetColumn(fillBdr, 0);
                progressGrid.Children.Add(fillBdr);

                if (safePct < 100)
                {
                    var emptyBdr = new Border
                    {
                        CornerRadius = new CornerRadius(5),
                        Background   = new SolidColorBrush(Color.FromRgb(220, 220, 230))
                    };
                    Grid.SetColumn(emptyBdr, 1);
                    progressGrid.Children.Add(emptyBdr);
                }

                sp.Children.Add(progressGrid);

                // Строки по статусам
                var statItems = new[]
                {
                    new[] { "П", "Присутствовал",       "#DFF6DD", "#107C10" },
                    new[] { "Н", "Отсутствовал",        "#FDE7E9", "#C43E1C" },
                    new[] { "О", "Опоздал",             "#FFF4CE", "#7A4F00" },
                    new[] { "Б", "Болезнь",             "#EFF6FC", "#0063B1" },
                    new[] { "У", "Уважительная причина","#F5F0FF", "#5B2D8E" },
                };

                foreach (var si in statItems)
                {
                    string code  = si[0], label = si[1], bg = si[2], fg = si[3];
                    int    cnt   = statusDict.ContainsKey(code) ? statusDict[code] : 0;
                    if (cnt == 0 && code != "П") continue;

                    var bgColor = (Color)ColorConverter.ConvertFromString(bg);
                    var fgColor = (Color)ColorConverter.ConvertFromString(fg);

                    var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var lbl = new TextBlock
                    {
                        Text              = label,
                        FontSize          = 12,
                        Foreground        = new SolidColorBrush(Color.FromRgb(90, 102, 122)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(lbl, 0);
                    row.Children.Add(lbl);

                    var cntBadge = MakeBadge(cnt.ToString(), bgColor, fgColor);
                    Grid.SetColumn(cntBadge, 1);
                    row.Children.Add(cntBadge);

                    sp.Children.Add(row);
                }

                // Итог
                sp.Children.Add(new Border
                {
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(238, 241, 247)),
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Margin          = new Thickness(0, 8, 0, 0),
                    Padding         = new Thickness(0, 8, 0, 0),
                    Child = new TextBlock
                    {
                        Text      = "Итого занятий: " + total + "  ·  Посещаемость: " + pct.ToString("F1") + "%",
                        FontSize  = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166))
                    }
                });

                summaryBorder.Child = sp;
                PanelAttendance.Children.Add(summaryBorder);
            }
            catch (Exception ex)
            {
                PanelAttendance.Children.Add(new TextBlock
                {
                    Text      = "Ошибка: " + ex.Message,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize  = 12
                });
            }
        }

        // ── Вкладка 5: Характеристика ──────────────────────────────────────

        private void LoadCharacteristic(int studentId)
        {
            TxtCharacteristic.Text     = "";
            TxtCharacteristicMeta.Text = "";
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetCharacteristic",
                    new[] { new SqlParameter("@StudentId", studentId) });

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    TxtCharacteristic.Text = row["CharacteristicText"]?.ToString() ?? "";

                    string updAt = row["UpdatedAt"] != DBNull.Value
                        ? Convert.ToDateTime(row["UpdatedAt"]).ToString("dd.MM.yyyy HH:mm")
                        : "";
                    string by = row["WrittenByName"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(updAt))
                        TxtCharacteristicMeta.Text = "Обновлено: " + updAt +
                            (string.IsNullOrEmpty(by) ? "" : " · " + by);
                    else
                        TxtCharacteristicMeta.Text = "Характеристика не заполнена";
                }
                else
                {
                    TxtCharacteristicMeta.Text = "Характеристика не заполнена";
                }
            }
            catch (Exception ex)
            {
                TxtCharacteristicMeta.Text = "Ошибка загрузки: " + ex.Message;
            }
        }

        private void BtnSaveCharacteristic_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null) return;
            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_SaveCharacteristic", new[]
                {
                    new SqlParameter("@StudentId",          _selectedStudent.StudentId),
                    new SqlParameter("@CharacteristicText", TxtCharacteristic.Text),
                    new SqlParameter("@WrittenById",        SessionHelper.UserId)
                });

                TxtCharacteristicMeta.Text =
                    "Обновлено: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                    " · " + SessionHelper.FullName;

                MessageBox.Show("Характеристика сохранена.", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Вспомогательные методы ─────────────────────────────────────────

        private static DropShadowEffect MakeShadow()
        {
            return new DropShadowEffect
            {
                Color       = Color.FromRgb(192, 202, 222),
                BlurRadius  = 8,
                ShadowDepth = 1,
                Opacity     = 0.12,
                Direction   = 270
            };
        }

        private static UIElement MakeEmptyNote(string icon, string message)
        {
            var sp = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 32, 0, 0)
            };
            sp.Children.Add(new TextBlock
            {
                Text                = icon,
                FontSize            = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 0, 0, 10)
            });
            sp.Children.Add(new TextBlock
            {
                Text                = message,
                FontSize            = 13,
                Foreground          = new SolidColorBrush(Color.FromRgb(138, 148, 166)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return sp;
        }

        private static Border MakeBadge(string text, Color bg, Color fg)
        {
            var b = new Border
            {
                Background   = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(10),
                Padding      = new Thickness(8, 2, 8, 2),
                Margin       = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            b.Child = new TextBlock
            {
                Text       = text,
                FontSize   = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(fg)
            };
            return b;
        }

        private static void GetCategoryColors(string category, out Color bg, out Color fg)
        {
            switch (category)
            {
                case "Олимпиады":
                    bg = Color.FromRgb(255, 244, 206); fg = Color.FromRgb(122,  79,   0); break;
                case "Наука":
                    bg = Color.FromRgb(239, 246, 252); fg = Color.FromRgb(  0,  63, 177); break;
                case "Спорт":
                    bg = Color.FromRgb(223, 246, 221); fg = Color.FromRgb( 16, 124,  16); break;
                case "Творчество":
                    bg = Color.FromRgb(245, 240, 255); fg = Color.FromRgb( 91,  45, 142); break;
                case "Общественная деятельность":
                    bg = Color.FromRgb(252, 240, 239); fg = Color.FromRgb(196,  62,  28); break;
                default:
                    bg = Color.FromRgb(240, 244, 251); fg = Color.FromRgb( 61,  74,  96); break;
            }
        }

        private static void GetGradeBadgeColors(double avg, out Color bg, out Color fg)
        {
            if      (avg >= 4.5) { bg = Color.FromRgb(223, 246, 221); fg = Color.FromRgb( 16, 124,  16); }
            else if (avg >= 3.5) { bg = Color.FromRgb(239, 246, 252); fg = Color.FromRgb(  0,  99, 177); }
            else if (avg >= 2.5) { bg = Color.FromRgb(255, 244, 206); fg = Color.FromRgb(122,  79,   0); }
            else                 { bg = Color.FromRgb(253, 231, 233); fg = Color.FromRgb(196,  62,  28); }
        }

        private static string FormatField(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col)) return "—";
            var v = row[col];
            return v == DBNull.Value || string.IsNullOrWhiteSpace(v.ToString()) ? "—" : v.ToString();
        }

        private static string FormatDate(DataRow row, string col)
        {
            if (!row.Table.Columns.Contains(col)) return "—";
            return row[col] == DBNull.Value ? "—" : Convert.ToDateTime(row[col]).ToString("dd.MM.yyyy");
        }
    }

    // ── Модели данных ──────────────────────────────────────────────────────────

    public class StudentListItem
    {
        public int    StudentId        { get; set; }
        public string FullName         { get; set; }
        public string ShortName        { get; set; }
        public string Initial          { get; set; }
        public string GroupName        { get; set; }
        public int    GroupId          { get; set; }
        public bool   IsHeadman        { get; set; }
        public string StudentCode      { get; set; }
        public int    AchievementCount { get; set; }
    }

    public class AchievementItem
    {
        public int       AchievementId  { get; set; }
        public string    Title          { get; set; }
        public string    Category       { get; set; }
        public string    Level          { get; set; }
        public string    Description    { get; set; }
        public string    DocumentNumber { get; set; }
        public string    AddedByName    { get; set; }
        public DateTime? AchieveDate    { get; set; }
    }
}
