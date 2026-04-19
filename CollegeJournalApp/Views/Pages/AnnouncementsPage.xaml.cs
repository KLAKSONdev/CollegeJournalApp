using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Pages
{
    public partial class AnnouncementsPage : Page
    {
        private List<AnnRow> _all = new List<AnnRow>();

        public AnnouncementsPage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => Init();
        }

        // ── Инициализация ──────────────────────────────────────────────────

        private void Init()
        {
            if (SessionHelper.IsAdmin)
                BtnCreate.Visibility = Visibility.Visible;

            LoadData();
        }

        // ── Загрузка данных ────────────────────────────────────────────────

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAnnouncements", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });
                _all.Clear();

                foreach (DataRow r in dt.Rows)
                {
                    _all.Add(new AnnRow
                    {
                        AnnouncementId  = Convert.ToInt32(r["AnnouncementId"]),
                        Title           = r["Title"]?.ToString()           ?? "",
                        Body            = r["Body"]?.ToString()            ?? "",
                        AuthorName      = r["AuthorName"]?.ToString()      ?? "—",
                        CreatedAt       = r["CreatedAt"] != DBNull.Value
                                          ? Convert.ToDateTime(r["CreatedAt"])
                                          : DateTime.MinValue,
                        ExpiresAt       = r["ExpiresAt"] != DBNull.Value
                                          ? (DateTime?)Convert.ToDateTime(r["ExpiresAt"])
                                          : null,
                        TargetAudience  = r["TargetAudience"]?.ToString()  ?? "all",
                        TargetGroupName = r["TargetGroupName"]?.ToString() ?? "",
                        TargetUserName  = r["TargetUserName"]?.ToString()  ?? ""
                    });
                }

                Render(TxtSearch.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Отрисовка карточек ─────────────────────────────────────────────

        private void Render(string search)
        {
            AnnouncementsPanel.Children.Clear();

            var list = string.IsNullOrWhiteSpace(search)
                ? _all
                : _all.Where(a =>
                    a.Title.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    a.Body.IndexOf(search,  StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            TxtCount.Text = $"— {list.Count} объявлений";

            if (list.Count == 0)
            {
                AnnouncementsPanel.Children.Add(BuildEmpty(
                    string.IsNullOrWhiteSpace(search)
                        ? "Объявлений пока нет"
                        : "Ничего не найдено по запросу «" + search + "»"));
                return;
            }

            foreach (var row in list)
                AnnouncementsPanel.Children.Add(BuildCard(row));
        }

        private UIElement BuildEmpty(string text)
        {
            var b = new Border
            {
                Background   = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding      = new Thickness(40),
                Margin       = new Thickness(0, 0, 0, 10)
            };
            b.Effect = Shadow();
            b.Child = new TextBlock
            {
                Text                = text,
                FontSize            = 14,
                Foreground          = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            return b;
        }

        private UIElement BuildCard(AnnRow row)
        {
            var card = new Border
            {
                Background   = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Margin       = new Thickness(0, 0, 0, 12)
            };
            card.Effect = Shadow();

            var outer = new Grid();
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Полоска слева — цвет зависит от аудитории
            var strip = new Border
            {
                Background   = new SolidColorBrush(AudienceAccentColor(row.TargetAudience)),
                CornerRadius = new CornerRadius(10, 0, 0, 10)
            };
            Grid.SetColumn(strip, 0);
            outer.Children.Add(strip);

            // Контент
            var content = new StackPanel { Margin = new Thickness(18, 14, 18, 14) };
            Grid.SetColumn(content, 1);

            // Заголовок + кнопка удаления
            var titleRow = new Grid();
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleTb = new TextBlock
            {
                Text         = row.Title,
                FontSize     = 15,
                FontWeight   = FontWeights.SemiBold,
                Foreground   = new SolidColorBrush(Color.FromRgb(26, 26, 46)),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(titleTb, 0);
            titleRow.Children.Add(titleTb);

            if (SessionHelper.IsAdmin)
            {
                int capturedId = row.AnnouncementId;
                var btnDel = new Button
                {
                    Content           = "Удалить",
                    FontSize          = 11,
                    Height            = 26,
                    Padding           = new Thickness(10, 0, 10, 0),
                    Background        = new SolidColorBrush(Color.FromRgb(253, 232, 233)),
                    Foreground        = new SolidColorBrush(Color.FromRgb(164, 38, 44)),
                    BorderThickness   = new Thickness(0),
                    Cursor            = System.Windows.Input.Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Top
                };
                btnDel.Click += (s, e) => DeleteAnnouncement(capturedId);
                Grid.SetColumn(btnDel, 1);
                titleRow.Children.Add(btnDel);
            }

            content.Children.Add(titleRow);

            // Бейдж аудитории
            var badgeText = AudienceBadgeText(row);
            if (!string.IsNullOrEmpty(badgeText))
            {
                var badge = new Border
                {
                    Background    = new SolidColorBrush(AudienceBadgeBack(row.TargetAudience)),
                    CornerRadius  = new CornerRadius(4),
                    Padding       = new Thickness(7, 2, 7, 2),
                    Margin        = new Thickness(0, 6, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                badge.Child = new TextBlock
                {
                    Text       = badgeText,
                    FontSize   = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(AudienceAccentColor(row.TargetAudience))
                };
                content.Children.Add(badge);
            }

            // Тело объявления
            content.Children.Add(new TextBlock
            {
                Text         = row.Body,
                FontSize     = 13,
                Foreground   = new SolidColorBrush(Color.FromRgb(52, 58, 64)),
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 8, 0, 10),
                LineHeight   = 20
            });

            // Подвал: автор, дата создания и срок действия
            var footer = new TextBlock
            {
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };
            footer.Inlines.Add(new Run(row.AuthorName) { FontWeight = FontWeights.SemiBold });
            footer.Inlines.Add(new Run("  ·  "));
            footer.Inlines.Add(new Run(row.CreatedAt != DateTime.MinValue
                ? row.CreatedAt.ToString("dd MMMM yyyy, HH:mm",
                    new System.Globalization.CultureInfo("ru-RU"))
                : "—"));

            if (row.ExpiresAt.HasValue)
            {
                bool expiresSoon = row.ExpiresAt.Value.Date <= DateTime.Today.AddDays(3);
                var expColor = expiresSoon
                    ? Color.FromRgb(164, 38, 44)   // красный — скоро истекает
                    : Color.FromRgb(108, 117, 125); // серый — обычный

                footer.Inlines.Add(new Run("  ·  до "));
                footer.Inlines.Add(new Run(
                    row.ExpiresAt.Value.ToString("dd MMMM yyyy",
                        new System.Globalization.CultureInfo("ru-RU")))
                {
                    Foreground = new SolidColorBrush(expColor),
                    FontWeight = expiresSoon ? FontWeights.SemiBold : FontWeights.Normal
                });
            }

            content.Children.Add(footer);

            outer.Children.Add(content);
            card.Child = outer;
            return card;
        }

        // ── Вспомогательные методы для аудитории ──────────────────────────

        private static string AudienceBadgeText(AnnRow row)
        {
            switch (row.TargetAudience)
            {
                case "all":      return "";
                case "students": return "👨‍🎓  Студентам";
                case "teachers": return "👨‍🏫  Учителям";
                case "curators": return "👨‍💼  Кураторам";
                case "headmen":  return "⭐  Старостам";
                case "group":    return "👥  Группа: " + (string.IsNullOrEmpty(row.TargetGroupName) ? "—" : row.TargetGroupName);
                case "user":     return "👤  " + (string.IsNullOrEmpty(row.TargetUserName) ? "—" : row.TargetUserName);
                default:         return "";
            }
        }

        private static Color AudienceAccentColor(string audience)
        {
            switch (audience)
            {
                case "students": return Color.FromRgb(16,  124, 16);   // зелёный
                case "teachers": return Color.FromRgb(135, 100, 184);  // фиолетовый
                case "curators": return Color.FromRgb(202, 80,  16);   // оранжевый
                case "headmen":  return Color.FromRgb(193, 156, 0);    // золотой
                case "group":    return Color.FromRgb(0,   153, 188);  // голубой
                case "user":     return Color.FromRgb(227, 0,   140);  // розовый
                default:         return Color.FromRgb(0,   120, 212);  // синий (all)
            }
        }

        private static Color AudienceBadgeBack(string audience)
        {
            var c = AudienceAccentColor(audience);
            // Очень светлый оттенок того же цвета
            return Color.FromArgb(25, c.R, c.G, c.B);
        }

        private static DropShadowEffect Shadow() => new DropShadowEffect
        {
            Color       = Color.FromRgb(26, 43, 74),
            Opacity     = 0.08,
            BlurRadius  = 10,
            ShadowDepth = 2,
            Direction   = 270
        };

        // ── Создать объявление ─────────────────────────────────────────────

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            ShowCreateDialog();
        }

        private void ShowCreateDialog()
        {
            // ── Загружаем группы и пользователей для выпадающих списков ───
            var groups = new List<IdName>();
            var users  = new List<IdName>();
            try
            {
                var dtGroups = DatabaseHelper.ExecuteProcedure("sp_GetAllGroups", null);
                foreach (DataRow r in dtGroups.Rows)
                    groups.Add(new IdName(Convert.ToInt32(r["GroupId"]), r["GroupName"]?.ToString() ?? ""));
            }
            catch { }
            try
            {
                var dtUsers = DatabaseHelper.ExecuteProcedure("sp_GetAllActiveUsers", null);
                foreach (DataRow r in dtUsers.Rows)
                    users.Add(new IdName(Convert.ToInt32(r["UserId"]), r["FullName"]?.ToString() ?? ""));
            }
            catch { }

            // ── Окно ──────────────────────────────────────────────────────
            var win = new Window
            {
                Title                 = "Новое объявление",
                Width                 = 500,
                Height                = 560,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner                 = Window.GetWindow(this),
                ResizeMode            = ResizeMode.NoResize,
                FontFamily            = new FontFamily("Segoe UI"),
                Background            = new SolidColorBrush(Color.FromRgb(250, 249, 248))
            };

            var root = new StackPanel { Margin = new Thickness(24) };

            // ── Заголовок ─────────────────────────────────────────────────
            root.Children.Add(MakeLabel("Заголовок"));
            var txtTitle = new TextBox
            {
                Height      = 32,
                FontSize    = 13,
                Padding     = new Thickness(10, 6, 10, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                Margin      = new Thickness(0, 0, 0, 12)
            };
            root.Children.Add(txtTitle);

            // ── Текст ─────────────────────────────────────────────────────
            root.Children.Add(MakeLabel("Текст объявления"));
            var txtBody = new TextBox
            {
                Height                      = 100,
                FontSize                    = 13,
                Padding                     = new Thickness(10, 8, 10, 8),
                BorderBrush                 = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                TextWrapping                = TextWrapping.Wrap,
                AcceptsReturn               = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin                      = new Thickness(0, 0, 0, 12)
            };
            root.Children.Add(txtBody);

            // ── Получатели — индексы: 0=все 1=студенты 2=учителя 3=кураторы 4=старосты 5=группа 6=пользователь
            root.Children.Add(MakeLabel("Получатели"));
            var cmbAudience = new ComboBox
            {
                Height   = 32,
                FontSize = 13,
                Padding  = new Thickness(8, 0, 8, 0),
                Margin   = new Thickness(0, 0, 0, 8)
            };
            foreach (var name in new[] { "Всем", "Студентам", "Учителям", "Кураторам", "Старостам",
                                          "Конкретной группе", "Конкретному пользователю" })
                cmbAudience.Items.Add(name);
            root.Children.Add(cmbAudience);

            // ── Выбор группы (скрыт по умолчанию) ────────────────────────
            var panelGroup = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };
            panelGroup.Children.Add(MakeLabel("Группа"));
            var cmbGroup = new ComboBox { Height = 32, FontSize = 13, Padding = new Thickness(8, 0, 8, 0) };
            foreach (var g in groups)
                cmbGroup.Items.Add(g.Name);
            if (groups.Count > 0) cmbGroup.SelectedIndex = 0;
            panelGroup.Children.Add(cmbGroup);
            root.Children.Add(panelGroup);

            // ── Выбор пользователя (скрыт по умолчанию) ──────────────────
            var panelUser = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };
            panelUser.Children.Add(MakeLabel("Пользователь"));
            var cmbUser = new ComboBox { Height = 32, FontSize = 13, Padding = new Thickness(8, 0, 8, 0) };
            foreach (var u in users)
                cmbUser.Items.Add(u.Name);
            if (users.Count > 0) cmbUser.SelectedIndex = 0;
            panelUser.Children.Add(cmbUser);
            root.Children.Add(panelUser);

            // ── Устанавливаем начальный выбор ПОСЛЕ подписки на событие ───
            cmbAudience.SelectionChanged += (s, e) =>
            {
                int idx = cmbAudience.SelectedIndex;
                panelGroup.Visibility = idx == 5 ? Visibility.Visible : Visibility.Collapsed;
                panelUser.Visibility  = idx == 6 ? Visibility.Visible : Visibility.Collapsed;
            };
            cmbAudience.SelectedIndex = 0;

            // ── Срок действия ─────────────────────────────────────────────
            var chkExpires = new CheckBox
            {
                Content   = "Установить срок действия",
                FontSize  = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 58, 64)),
                Margin    = new Thickness(0, 10, 0, 0)
            };
            root.Children.Add(chkExpires);

            var panelExpires = new StackPanel
            {
                Visibility = Visibility.Collapsed,
                Margin     = new Thickness(0, 8, 0, 0)
            };

            // Строка: дата + время
            panelExpires.Children.Add(MakeLabel("Действует до (дата и время)"));

            var expiresRow = new StackPanel { Orientation = Orientation.Horizontal };

            var dpExpires = new DatePicker
            {
                Width            = 148,
                FontSize         = 12,
                SelectedDate     = DateTime.Today.AddDays(7),
                DisplayDateStart = DateTime.Today,
                VerticalAlignment = VerticalAlignment.Center,
                Margin           = new Thickness(0, 0, 6, 0)
            };
            expiresRow.Children.Add(dpExpires);

            // Часы
            var cmbHour = new ComboBox
            {
                Width             = 56,
                FontSize          = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 2, 0)
            };
            for (int h = 0; h < 24; h++)
                cmbHour.Items.Add(h.ToString("00"));
            cmbHour.SelectedIndex = 23; // 23:00 по умолчанию
            expiresRow.Children.Add(cmbHour);

            expiresRow.Children.Add(new TextBlock
            {
                Text              = ":",
                FontSize          = 14,
                FontWeight        = FontWeights.Bold,
                Foreground        = new SolidColorBrush(Color.FromRgb(52, 58, 64)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 2, 0)
            });

            // Минуты (шаг 5)
            var minuteValues = new List<int>();
            for (int m = 0; m < 60; m += 5) minuteValues.Add(m);

            var cmbMinute = new ComboBox
            {
                Width             = 56,
                FontSize          = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            foreach (var m in minuteValues)
                cmbMinute.Items.Add(m.ToString("00"));
            cmbMinute.SelectedIndex = 11; // :55 по умолчанию
            expiresRow.Children.Add(cmbMinute);

            panelExpires.Children.Add(expiresRow);

            // Подсказка: сколько осталось
            var lblTimeLeft = new TextBlock
            {
                FontSize   = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Margin     = new Thickness(0, 4, 0, 0)
            };
            panelExpires.Children.Add(lblTimeLeft);

            // Обновляем подсказку при изменении даты/времени
            Action updateHint = () =>
            {
                if (dpExpires.SelectedDate == null) { lblTimeLeft.Text = ""; return; }
                int h = cmbHour.SelectedIndex < 0 ? 0 : cmbHour.SelectedIndex;
                int m = cmbMinute.SelectedIndex < 0 ? 0 : minuteValues[cmbMinute.SelectedIndex];
                var dt = dpExpires.SelectedDate.Value.Date.AddHours(h).AddMinutes(m);
                var diff = dt - DateTime.Now;
                if (diff <= TimeSpan.Zero)
                    lblTimeLeft.Text = "⚠ Время уже истекло";
                else if (diff.TotalHours < 1)
                    lblTimeLeft.Text = $"Истечёт через {(int)diff.TotalMinutes} мин.";
                else if (diff.TotalDays < 1)
                    lblTimeLeft.Text = $"Истечёт через {(int)diff.TotalHours} ч. {diff.Minutes} мин.";
                else
                    lblTimeLeft.Text = $"Истечёт через {(int)diff.TotalDays} дн. {diff.Hours} ч.";
            };

            dpExpires.SelectedDateChanged += (s, e) => updateHint();
            cmbHour.SelectionChanged      += (s, e) => updateHint();
            cmbMinute.SelectionChanged    += (s, e) => updateHint();
            updateHint();

            root.Children.Add(panelExpires);

            chkExpires.Checked   += (s, e) => panelExpires.Visibility = Visibility.Visible;
            chkExpires.Unchecked += (s, e) => panelExpires.Visibility = Visibility.Collapsed;

            // ── Кнопки ────────────────────────────────────────────────────
            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin              = new Thickness(0, 16, 0, 0)
            };

            var btnCancel = new Button
            {
                Content         = "Отмена",
                Height          = 34,
                Padding         = new Thickness(18, 0, 18, 0),
                Background      = Brushes.Transparent,
                BorderBrush     = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                BorderThickness = new Thickness(1),
                FontSize        = 12,
                Foreground      = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                Margin          = new Thickness(0, 0, 8, 0),
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => win.Close();
            btnRow.Children.Add(btnCancel);

            var btnSave = new Button
            {
                Content         = "Опубликовать",
                Height          = 34,
                Padding         = new Thickness(18, 0, 18, 0),
                Background      = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                Foreground      = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize        = 12,
                FontWeight      = FontWeights.SemiBold,
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            btnSave.Click += (s, e) =>
            {
                var title = txtTitle.Text.Trim();
                var body  = txtBody.Text.Trim();

                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Введите заголовок объявления.", "Проверка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtTitle.Focus();
                    return;
                }
                if (string.IsNullOrEmpty(body))
                {
                    MessageBox.Show("Введите текст объявления.", "Проверка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtBody.Focus();
                    return;
                }

                // SelectedIndex: 0=все 1=студенты 2=учителя 3=кураторы 4=старосты 5=группа 6=пользователь
                int    idx           = cmbAudience.SelectedIndex;
                string targetAudience;
                int?   targetGroupId = null;
                int?   targetUserId  = null;

                switch (idx)
                {
                    case 1:  targetAudience = "students"; break;
                    case 2:  targetAudience = "teachers"; break;
                    case 3:  targetAudience = "curators"; break;
                    case 4:  targetAudience = "headmen";  break;
                    case 5:
                        if (cmbGroup.SelectedIndex < 0 || cmbGroup.SelectedIndex >= groups.Count)
                        {
                            MessageBox.Show("Выберите группу.", "Проверка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        targetAudience = "group";
                        targetGroupId  = groups[cmbGroup.SelectedIndex].Id;
                        break;
                    case 6:
                        if (cmbUser.SelectedIndex < 0 || cmbUser.SelectedIndex >= users.Count)
                        {
                            MessageBox.Show("Выберите пользователя.", "Проверка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        targetAudience = "user";
                        targetUserId   = users[cmbUser.SelectedIndex].Id;
                        break;
                    default: targetAudience = "all"; break;
                }

                // Срок действия
                object expiresVal = DBNull.Value;
                if (chkExpires.IsChecked == true && dpExpires.SelectedDate.HasValue)
                    expiresVal = dpExpires.SelectedDate.Value.Date;

                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_CreateAnnouncement", new[]
                    {
                        new SqlParameter("@Title",          title),
                        new SqlParameter("@Body",           body),
                        new SqlParameter("@AuthorId",       SessionHelper.UserId),
                        new SqlParameter("@TargetAudience", targetAudience),
                        new SqlParameter("@TargetGroupId",  (object)targetGroupId ?? DBNull.Value),
                        new SqlParameter("@TargetUserId",   (object)targetUserId  ?? DBNull.Value),
                        new SqlParameter("@ExpiresAt",      expiresVal)
                    });
                    win.DialogResult = true;
                    win.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения:\n" + ex.Message,
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnRow.Children.Add(btnSave);
            root.Children.Add(btnRow);

            win.Content = root;
            if (win.ShowDialog() == true)
                LoadData();
        }

        private static TextBlock MakeLabel(string text) => new TextBlock
        {
            Text       = text,
            FontSize   = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
            Margin     = new Thickness(0, 0, 0, 4)
        };

        // ── Удалить объявление ─────────────────────────────────────────────

        private void DeleteAnnouncement(int id)
        {
            if (MessageBox.Show("Удалить это объявление?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_DeleteAnnouncement", new[]
                {
                    new SqlParameter("@AnnouncementId", id),
                    new SqlParameter("@DeletedById",    SessionHelper.UserId)
                });
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Поиск ──────────────────────────────────────────────────────────

        private void TxtSearch_Changed(object sender, TextChangedEventArgs e)
        {
            Render(TxtSearch.Text.Trim());
        }
    }

    // ── Вспомогательные классы ─────────────────────────────────────────────

    public class AnnRow
    {
        public int       AnnouncementId  { get; set; }
        public string    Title           { get; set; }
        public string    Body            { get; set; }
        public string    AuthorName      { get; set; }
        public DateTime  CreatedAt       { get; set; }
        public DateTime? ExpiresAt       { get; set; }
        public string    TargetAudience  { get; set; }
        public string    TargetGroupName { get; set; }
        public string    TargetUserName  { get; set; }
    }

    internal class IdName
    {
        public int    Id   { get; }
        public string Name { get; }
        public IdName(int id, string name) { Id = id; Name = name; }
    }
}
