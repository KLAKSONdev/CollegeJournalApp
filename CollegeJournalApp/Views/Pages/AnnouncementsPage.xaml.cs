using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CollegeJournalApp.Views.Pages
{
    public partial class AnnouncementsPage : Page
    {
        private List<AnnRow>    _all   = new List<AnnRow>();
        private DispatcherTimer _timer;

        public AnnouncementsPage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded   += (s, e) => Init();
            Unloaded += (s, e) => _timer?.Stop();
        }

        // ── Инициализация ──────────────────────────────────────────────────

        private void Init()
        {
            if (SessionHelper.IsAdmin)
                BtnCreate.Visibility = Visibility.Visible;

            LoadData();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _timer.Tick += (s, e) => LoadData();
            _timer.Start();
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
                        TargetUserName  = r["TargetUserName"]?.ToString()  ?? "",
                        IsPinnedByUser  = r.Table.Columns.Contains("IsPinnedByUser")
                                          && r["IsPinnedByUser"] != DBNull.Value
                                          && Convert.ToInt32(r["IsPinnedByUser"]) == 1,
                        AttachmentCount = r.Table.Columns.Contains("AttachmentCount")
                                          && r["AttachmentCount"] != DBNull.Value
                                          ? Convert.ToInt32(r["AttachmentCount"]) : 0
                    });
                }

                // Загружаем вложения (метаданные)
                LoadAttachmentsMeta();

                Render(TxtSearch.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAttachmentsMeta()
        {
            if (_all.Count == 0) return;
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAnnouncementAttachmentsMeta", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });

                // Группируем по AnnouncementId
                var dict = new Dictionary<int, List<AnnAttachment>>();
                foreach (DataRow r in dt.Rows)
                {
                    int annId = Convert.ToInt32(r["AnnouncementId"]);
                    if (!dict.ContainsKey(annId))
                        dict[annId] = new List<AnnAttachment>();
                    dict[annId].Add(new AnnAttachment
                    {
                        AttachmentId   = Convert.ToInt32(r["AttachmentId"]),
                        AnnouncementId = annId,
                        FileName       = r["FileName"]?.ToString()    ?? "файл",
                        FileSize       = r["FileSize"] != DBNull.Value
                                         ? Convert.ToInt64(r["FileSize"]) : 0,
                        ContentType    = r["ContentType"]?.ToString() ?? ""
                    });
                }

                foreach (var row in _all)
                {
                    if (dict.ContainsKey(row.AnnouncementId))
                        row.Attachments = dict[row.AnnouncementId];
                }
            }
            catch { /* вложения не критичны */ }
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

            // Закреплённые — вверх
            list = list.OrderByDescending(a => a.IsPinnedByUser)
                       .ThenByDescending(a => a.CreatedAt)
                       .ToList();

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
            {
                var card = BuildCard(row);
                if (card != null)
                    AnnouncementsPanel.Children.Add(card);
            }
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
            // Клиентская проверка срока (на случай устаревших данных)
            if (row.ExpiresAt.HasValue && row.ExpiresAt.Value <= DateTime.Now)
                return null;

            var card = new Border
            {
                Background   = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Margin       = new Thickness(0, 0, 0, 12)
            };
            card.Effect = Shadow();

            // Если закреплено — лёгкая обводка
            if (row.IsPinnedByUser)
            {
                card.BorderBrush     = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                card.BorderThickness = new Thickness(1.5);
            }

            var outer = new Grid();
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Полоска слева
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

            // ── Строка заголовка: [закреп] [заголовок] [кнопки] ──────────
            var titleRow = new Grid();
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });   // иконка закрепа
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // заголовок
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });   // кнопки

            // Иконка «закреплено»
            if (row.IsPinnedByUser)
            {
                var pinIcon = new TextBlock
                {
                    Text              = "📌",
                    FontSize          = 13,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin            = new Thickness(0, 2, 6, 0),
                    ToolTip           = "Закреплено вами"
                };
                Grid.SetColumn(pinIcon, 0);
                titleRow.Children.Add(pinIcon);
            }

            var titleTb = new TextBlock
            {
                Text         = row.Title,
                FontSize     = 15,
                FontWeight   = FontWeights.SemiBold,
                Foreground   = new SolidColorBrush(Color.FromRgb(26, 26, 46)),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(titleTb, 1);
            titleRow.Children.Add(titleTb);

            // Кнопки управления
            var btnPanel = new StackPanel
            {
                Orientation       = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(btnPanel, 2);

            // Кнопка закрепить/открепить
            int  capturedId    = row.AnnouncementId;
            bool capturedPin   = row.IsPinnedByUser;
            var btnPin = new Button
            {
                Content         = row.IsPinnedByUser ? "📌 Открепить" : "📌 Закрепить",
                FontSize        = 11,
                Height          = 26,
                Padding         = new Thickness(8, 0, 8, 0),
                Background      = row.IsPinnedByUser
                                  ? new SolidColorBrush(Color.FromArgb(30, 0, 120, 212))
                                  : new SolidColorBrush(Color.FromRgb(243, 242, 241)),
                Foreground      = row.IsPinnedByUser
                                  ? new SolidColorBrush(Color.FromRgb(0, 90, 158))
                                  : new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                BorderThickness = new Thickness(0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                Margin          = new Thickness(0, 0, 6, 0),
                ToolTip         = row.IsPinnedByUser ? "Открепить объявление" : "Закрепить для себя"
            };
            btnPin.Click += (s, e) => TogglePin(capturedId, capturedPin);
            btnPanel.Children.Add(btnPin);

            // Кнопка удаления (только Admin)
            if (SessionHelper.IsAdmin)
            {
                var btnDel = new Button
                {
                    Content         = "Удалить",
                    FontSize        = 11,
                    Height          = 26,
                    Padding         = new Thickness(10, 0, 10, 0),
                    Background      = new SolidColorBrush(Color.FromRgb(253, 232, 233)),
                    Foreground      = new SolidColorBrush(Color.FromRgb(164, 38, 44)),
                    BorderThickness = new Thickness(0),
                    Cursor          = System.Windows.Input.Cursors.Hand
                };
                btnDel.Click += (s, e) => DeleteAnnouncement(capturedId);
                btnPanel.Children.Add(btnDel);
            }

            titleRow.Children.Add(btnPanel);
            content.Children.Add(titleRow);

            // Бейдж аудитории
            var badgeText = AudienceBadgeText(row);
            if (!string.IsNullOrEmpty(badgeText))
            {
                var badge = new Border
                {
                    Background          = new SolidColorBrush(AudienceBadgeBack(row.TargetAudience)),
                    CornerRadius        = new CornerRadius(4),
                    Padding             = new Thickness(7, 2, 7, 2),
                    Margin              = new Thickness(0, 6, 0, 0),
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

            // ── Вложения ───────────────────────────────────────────────────
            if (row.Attachments != null && row.Attachments.Count > 0)
            {
                var attHeader = new TextBlock
                {
                    Text       = $"📎  Вложения ({row.Attachments.Count})",
                    FontSize   = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                    Margin     = new Thickness(0, 0, 0, 4)
                };
                content.Children.Add(attHeader);

                var attPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
                foreach (var att in row.Attachments)
                {
                    int    attId      = att.AttachmentId;
                    string attName    = att.FileName;
                    string attSize    = FormatFileSize(att.FileSize);
                    string attIcon    = GetFileIcon(att.FileName);

                    var chip = new Border
                    {
                        Background      = new SolidColorBrush(Color.FromRgb(243, 242, 241)),
                        BorderBrush     = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        BorderThickness = new Thickness(1),
                        CornerRadius    = new CornerRadius(4),
                        Padding         = new Thickness(8, 4, 8, 4),
                        Margin          = new Thickness(0, 0, 6, 6),
                        Cursor          = System.Windows.Input.Cursors.Hand,
                        ToolTip         = $"{attName}  ({attSize})  — нажмите для открытия"
                    };

                    var chipRow = new StackPanel { Orientation = Orientation.Horizontal };
                    chipRow.Children.Add(new TextBlock
                    {
                        Text              = attIcon,
                        FontSize          = 14,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin            = new Thickness(0, 0, 5, 0)
                    });
                    chipRow.Children.Add(new TextBlock
                    {
                        Text              = attName.Length > 28 ? attName.Substring(0, 25) + "…" : attName,
                        FontSize          = 11,
                        Foreground        = new SolidColorBrush(Color.FromRgb(0, 90, 158)),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    chipRow.Children.Add(new TextBlock
                    {
                        Text              = "  " + attSize,
                        FontSize          = 10,
                        Foreground        = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    chip.Child     = chipRow;
                    chip.MouseLeftButtonUp += (s, e) => DownloadAttachment(attId, attName);
                    chip.MouseEnter += (s, e) =>
                        chip.Background = new SolidColorBrush(Color.FromRgb(225, 240, 255));
                    chip.MouseLeave += (s, e) =>
                        chip.Background = new SolidColorBrush(Color.FromRgb(243, 242, 241));

                    attPanel.Children.Add(chip);
                }
                content.Children.Add(attPanel);
            }

            // Подвал: автор + дата + срок
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
                var diff = row.ExpiresAt.Value - DateTime.Now;
                bool expiresSoon = diff.TotalHours <= 24;
                var expColor = expiresSoon
                    ? Color.FromRgb(164, 38, 44)
                    : Color.FromRgb(108, 117, 125);

                var expires = row.ExpiresAt.Value;
                bool hasTime = expires.Hour != 0 || expires.Minute != 0;
                string expiresStr = hasTime
                    ? expires.ToString("dd MMMM yyyy, HH:mm",
                          new System.Globalization.CultureInfo("ru-RU"))
                    : expires.ToString("dd MMMM yyyy",
                          new System.Globalization.CultureInfo("ru-RU"));

                string leftStr = "";
                if (diff.TotalMinutes < 60)
                    leftStr = $" (осталось {(int)diff.TotalMinutes} мин.)";
                else if (diff.TotalHours < 24)
                    leftStr = $" (осталось {(int)diff.TotalHours} ч. {diff.Minutes} мин.)";

                footer.Inlines.Add(new Run("  ·  до "));
                footer.Inlines.Add(new Run(expiresStr + leftStr)
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

        // ── Закрепить / Открепить ──────────────────────────────────────────

        private void TogglePin(int announcementId, bool currentlyPinned)
        {
            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_ToggleAnnPin", new[]
                {
                    new SqlParameter("@AnnouncementId", announcementId),
                    new SqlParameter("@UserId",         SessionHelper.UserId)
                });
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при закреплении:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Скачать вложение ───────────────────────────────────────────────

        private void DownloadAttachment(int attachmentId, string fileName)
        {
            try
            {
                var row = DatabaseHelper.ExecuteSingleRow("sp_DownloadAttachment", new[]
                {
                    new SqlParameter("@AttachmentId", attachmentId)
                });

                if (row == null || row["FileData"] == DBNull.Value)
                {
                    MessageBox.Show("Файл не найден.", "Вложение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var bytes   = (byte[])row["FileData"];
                var tempDir = Path.Combine(Path.GetTempPath(), "EduTrackAttachments");
                Directory.CreateDirectory(tempDir);
                var tempPath = Path.Combine(tempDir, fileName);

                File.WriteAllBytes(tempPath, bytes);

                try { Process.Start(tempPath); }
                catch
                {
                    // Если ОС не знает, чем открыть — показываем диалог «Открыть с помощью»
                    Process.Start(new ProcessStartInfo
                    {
                        FileName  = tempPath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки файла:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                case "students": return Color.FromRgb(16,  124, 16);
                case "teachers": return Color.FromRgb(135, 100, 184);
                case "curators": return Color.FromRgb(202, 80,  16);
                case "headmen":  return Color.FromRgb(193, 156, 0);
                case "group":    return Color.FromRgb(0,   153, 188);
                case "user":     return Color.FromRgb(227, 0,   140);
                default:         return Color.FromRgb(0,   120, 212);
            }
        }

        private static Color AudienceBadgeBack(string audience)
        {
            var c = AudienceAccentColor(audience);
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

        // ── Форматирование файлов ──────────────────────────────────────────

        private static string FormatFileSize(long bytes)
        {
            if (bytes <= 0)    return "0 Б";
            if (bytes < 1024)  return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} КБ";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1048576.0:F1} МБ";
            return $"{bytes / 1073741824.0:F1} ГБ";
        }

        private static string GetFileIcon(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
            switch (ext)
            {
                case ".pdf":                       return "📄";
                case ".doc":  case ".docx":        return "📝";
                case ".xls":  case ".xlsx":        return "📊";
                case ".ppt":  case ".pptx":        return "📑";
                case ".jpg":  case ".jpeg":
                case ".png":  case ".gif":
                case ".bmp":  case ".webp":        return "🖼";
                case ".zip":  case ".rar":
                case ".7z":   case ".tar":         return "🗜";
                case ".txt":                       return "📃";
                case ".mp4":  case ".avi":
                case ".mov":  case ".mkv":         return "🎬";
                case ".mp3":  case ".wav":
                case ".ogg":  case ".flac":        return "🎵";
                default:                           return "📎";
            }
        }

        private static string GetContentType(string filePath)
        {
            var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? "";
            switch (ext)
            {
                case ".pdf":  return "application/pdf";
                case ".doc":  return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls":  return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".ppt":  return "application/vnd.ms-powerpoint";
                case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".jpg":  case ".jpeg": return "image/jpeg";
                case ".png":  return "image/png";
                case ".gif":  return "image/gif";
                case ".txt":  return "text/plain";
                case ".zip":  return "application/zip";
                case ".rar":  return "application/x-rar-compressed";
                default:      return "application/octet-stream";
            }
        }

        // ── Создать объявление ─────────────────────────────────────────────

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            ShowCreateDialog();
        }

        private void ShowCreateDialog()
        {
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

            // Список прикрепляемых файлов
            var pendingFiles = new List<PendingFile>();

            var win = new Window
            {
                Title                 = "Новое объявление",
                Width                 = 520,
                Height                = 640,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner                 = Window.GetWindow(this),
                ResizeMode            = ResizeMode.NoResize,
                FontFamily            = new FontFamily("Segoe UI"),
                Background            = new SolidColorBrush(Color.FromRgb(250, 249, 248))
            };

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(0)
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
                Height                      = 90,
                FontSize                    = 13,
                Padding                     = new Thickness(10, 8, 10, 8),
                BorderBrush                 = new SolidColorBrush(Color.FromRgb(208, 208, 208)),
                TextWrapping                = TextWrapping.Wrap,
                AcceptsReturn               = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin                      = new Thickness(0, 0, 0, 12)
            };
            root.Children.Add(txtBody);

            // ── Получатели ────────────────────────────────────────────────
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

            var panelGroup = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };
            panelGroup.Children.Add(MakeLabel("Группа"));
            var cmbGroup = new ComboBox { Height = 32, FontSize = 13, Padding = new Thickness(8, 0, 8, 0) };
            foreach (var g in groups) cmbGroup.Items.Add(g.Name);
            if (groups.Count > 0) cmbGroup.SelectedIndex = 0;
            panelGroup.Children.Add(cmbGroup);
            root.Children.Add(panelGroup);

            var panelUser = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };
            panelUser.Children.Add(MakeLabel("Пользователь"));
            var cmbUser = new ComboBox { Height = 32, FontSize = 13, Padding = new Thickness(8, 0, 8, 0) };
            foreach (var u in users) cmbUser.Items.Add(u.Name);
            if (users.Count > 0) cmbUser.SelectedIndex = 0;
            panelUser.Children.Add(cmbUser);
            root.Children.Add(panelUser);

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
                Content    = "Установить срок действия",
                FontSize   = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(52, 58, 64)),
                Margin     = new Thickness(0, 10, 0, 0)
            };
            root.Children.Add(chkExpires);

            var panelExpires = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };
            panelExpires.Children.Add(MakeLabel("Действует до (дата и время)"));

            var expiresRow = new StackPanel { Orientation = Orientation.Horizontal };
            var dpExpires = new DatePicker
            {
                Width             = 148,
                FontSize          = 12,
                SelectedDate      = DateTime.Today.AddDays(7),
                DisplayDateStart  = DateTime.Today,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 6, 0)
            };
            expiresRow.Children.Add(dpExpires);

            var cmbHour = new ComboBox
            {
                Width             = 56,
                FontSize          = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 2, 0)
            };
            for (int h = 0; h < 24; h++) cmbHour.Items.Add(h.ToString("00"));
            cmbHour.SelectedIndex = 23;
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

            var minuteValues = new List<int>();
            for (int m = 0; m < 60; m += 5) minuteValues.Add(m);
            var cmbMinute = new ComboBox { Width = 56, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            foreach (var m in minuteValues) cmbMinute.Items.Add(m.ToString("00"));
            cmbMinute.SelectedIndex = 11;
            expiresRow.Children.Add(cmbMinute);

            panelExpires.Children.Add(expiresRow);

            var lblTimeLeft = new TextBlock
            {
                FontSize   = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Margin     = new Thickness(0, 4, 0, 0)
            };
            panelExpires.Children.Add(lblTimeLeft);

            Action updateHint = () =>
            {
                if (dpExpires.SelectedDate == null) { lblTimeLeft.Text = ""; return; }
                int h  = cmbHour.SelectedIndex < 0 ? 0 : cmbHour.SelectedIndex;
                int m  = cmbMinute.SelectedIndex < 0 ? 0 : minuteValues[cmbMinute.SelectedIndex];
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

            // ── Вложения ──────────────────────────────────────────────────
            var sepLine = new Border
            {
                Height          = 1,
                Background      = new SolidColorBrush(Color.FromRgb(228, 228, 228)),
                Margin          = new Thickness(0, 14, 0, 12)
            };
            root.Children.Add(sepLine);

            var attHeaderRow = new Grid();
            attHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            attHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            attHeaderRow.Children.Add(MakeLabel("Вложения"));

            var btnAddFile = new Button
            {
                Content         = "＋ Прикрепить файл",
                FontSize        = 11,
                Height          = 26,
                Padding         = new Thickness(10, 0, 10, 0),
                Background      = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                Foreground      = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(btnAddFile, 1);
            attHeaderRow.Children.Add(btnAddFile);
            root.Children.Add(attHeaderRow);

            // Список прикреплённых файлов
            var fileListPanel = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
            root.Children.Add(fileListPanel);

            // Лямбда для обновления списка файлов в UI
            Action refreshFileList = null;
            refreshFileList = () =>
            {
                fileListPanel.Children.Clear();
                foreach (var pf in pendingFiles.ToList())
                {
                    var pf2 = pf; // capture
                    var fileRow = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                    fileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    fileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    fileRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var iconTb = new TextBlock
                    {
                        Text              = GetFileIcon(pf2.FilePath),
                        FontSize          = 14,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin            = new Thickness(0, 0, 6, 0)
                    };
                    Grid.SetColumn(iconTb, 0);
                    fileRow.Children.Add(iconTb);

                    var nameTb = new TextBlock
                    {
                        Text              = $"{pf2.FileName}  ({FormatFileSize(pf2.FileSize)})",
                        FontSize          = 11,
                        Foreground        = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming      = TextTrimming.CharacterEllipsis
                    };
                    Grid.SetColumn(nameTb, 1);
                    fileRow.Children.Add(nameTb);

                    var btnRemove = new Button
                    {
                        Content         = "✕",
                        FontSize        = 10,
                        Width           = 20,
                        Height          = 20,
                        Padding         = new Thickness(0),
                        Background      = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Foreground      = new SolidColorBrush(Color.FromRgb(164, 38, 44)),
                        Cursor          = System.Windows.Input.Cursors.Hand,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin          = new Thickness(6, 0, 0, 0)
                    };
                    Grid.SetColumn(btnRemove, 2);
                    fileRow.Children.Add(btnRemove);

                    // Capture для удаления
                    var capturedPf = pf2;
                    btnRemove.Click += (s, e) =>
                    {
                        pendingFiles.Remove(capturedPf);
                        refreshFileList();
                    };

                    fileListPanel.Children.Add(fileRow);
                }

                if (pendingFiles.Count == 0)
                {
                    fileListPanel.Children.Add(new TextBlock
                    {
                        Text       = "Файлы не прикреплены",
                        FontSize   = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                        Margin     = new Thickness(0, 2, 0, 0)
                    });
                }
            };
            refreshFileList();

            btnAddFile.Click += (s, e) =>
            {
                if (pendingFiles.Count >= 5)
                {
                    MessageBox.Show("Можно прикрепить не более 5 файлов.", "Ограничение",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new OpenFileDialog
                {
                    Title       = "Выберите файл для вложения",
                    Filter      = "Все файлы|*.*|PDF|*.pdf|Word|*.doc;*.docx|Excel|*.xls;*.xlsx|Изображения|*.jpg;*.jpeg;*.png;*.gif",
                    Multiselect = true
                };
                if (dlg.ShowDialog() != true) return;

                foreach (var path in dlg.FileNames)
                {
                    if (pendingFiles.Count >= 5) break;
                    var fi = new FileInfo(path);
                    if (fi.Length > 20 * 1024 * 1024) // 20 МБ лимит
                    {
                        MessageBox.Show($"Файл «{fi.Name}» превышает допустимый размер 20 МБ.",
                            "Слишком большой файл", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }
                    // Проверяем дубликат
                    if (pendingFiles.Any(f => f.FileName == fi.Name)) continue;

                    pendingFiles.Add(new PendingFile
                    {
                        FilePath    = path,
                        FileName    = fi.Name,
                        FileSize    = fi.Length,
                        ContentType = GetContentType(path)
                    });
                }
                refreshFileList();
            };

            // ── Кнопки ────────────────────────────────────────────────────
            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin              = new Thickness(0, 16, 0, 4)
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
                    txtTitle.Focus(); return;
                }
                if (string.IsNullOrEmpty(body))
                {
                    MessageBox.Show("Введите текст объявления.", "Проверка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtBody.Focus(); return;
                }

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

                object expiresVal = DBNull.Value;
                if (chkExpires.IsChecked == true && dpExpires.SelectedDate.HasValue)
                {
                    int selHour = cmbHour.SelectedIndex >= 0 ? cmbHour.SelectedIndex : 23;
                    int selMin  = cmbMinute.SelectedIndex >= 0 ? minuteValues[cmbMinute.SelectedIndex] : 0;
                    var expiresDateTime = dpExpires.SelectedDate.Value.Date.AddHours(selHour).AddMinutes(selMin);
                    if (expiresDateTime <= DateTime.Now)
                    {
                        MessageBox.Show("Время истечения должно быть в будущем.", "Проверка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    expiresVal = expiresDateTime;
                }

                try
                {
                    // Создаём объявление и получаем новый ID
                    var newRow = DatabaseHelper.ExecuteSingleRow("sp_CreateAnnouncement", new[]
                    {
                        new SqlParameter("@Title",          title),
                        new SqlParameter("@Body",           body),
                        new SqlParameter("@AuthorId",       SessionHelper.UserId),
                        new SqlParameter("@TargetAudience", targetAudience),
                        new SqlParameter("@TargetGroupId",  (object)targetGroupId ?? DBNull.Value),
                        new SqlParameter("@TargetUserId",   (object)targetUserId  ?? DBNull.Value),
                        new SqlParameter("@ExpiresAt",      expiresVal)
                    });

                    // Загружаем вложения
                    if (pendingFiles.Count > 0 && newRow != null && newRow["AnnouncementId"] != DBNull.Value)
                    {
                        int newAnnId = Convert.ToInt32(newRow["AnnouncementId"]);
                        foreach (var pf in pendingFiles)
                        {
                            try
                            {
                                byte[] fileBytes = File.ReadAllBytes(pf.FilePath);
                                var attParam = new SqlParameter("@FileData", System.Data.SqlDbType.VarBinary, -1)
                                {
                                    Value = fileBytes
                                };
                                DatabaseHelper.ExecuteNonQuery("sp_AddAttachment", new[]
                                {
                                    new SqlParameter("@AnnouncementId", newAnnId),
                                    new SqlParameter("@FileName",       pf.FileName),
                                    new SqlParameter("@FileSize",       pf.FileSize),
                                    new SqlParameter("@ContentType",    pf.ContentType),
                                    attParam,
                                    new SqlParameter("@UploadedById",   SessionHelper.UserId)
                                });
                            }
                            catch (Exception ex2)
                            {
                                MessageBox.Show($"Не удалось загрузить файл «{pf.FileName}»:\n{ex2.Message}",
                                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }

                    win.DialogResult = true;
                    win.Close();
                }
                catch (Exception ex)
                {
                    var inner = ex.InnerException?.Message ?? "";
                    MessageBox.Show("Ошибка сохранения:\n" + ex.Message
                        + (inner.Length > 0 ? "\n\n[SQL] " + inner : ""),
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            btnRow.Children.Add(btnSave);
            root.Children.Add(btnRow);

            scroll.Content = root;
            win.Content    = scroll;

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
        public int                   AnnouncementId  { get; set; }
        public string                Title           { get; set; }
        public string                Body            { get; set; }
        public string                AuthorName      { get; set; }
        public DateTime              CreatedAt       { get; set; }
        public DateTime?             ExpiresAt       { get; set; }
        public string                TargetAudience  { get; set; }
        public string                TargetGroupName { get; set; }
        public string                TargetUserName  { get; set; }
        public bool                  IsPinnedByUser  { get; set; }
        public int                   AttachmentCount { get; set; }
        public List<AnnAttachment>   Attachments     { get; set; } = new List<AnnAttachment>();
    }

    public class AnnAttachment
    {
        public int    AttachmentId   { get; set; }
        public int    AnnouncementId { get; set; }
        public string FileName       { get; set; }
        public long   FileSize       { get; set; }
        public string ContentType    { get; set; }
    }

    internal class PendingFile
    {
        public string FilePath    { get; set; }
        public string FileName    { get; set; }
        public long   FileSize    { get; set; }
        public string ContentType { get; set; }
    }

    internal class IdName
    {
        public int    Id   { get; }
        public string Name { get; }
        public IdName(int id, string name) { Id = id; Name = name; }
    }
}
