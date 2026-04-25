using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using CollegeJournalApp.Views.Dialogs;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CollegeJournalApp.Views.Pages
{
    public partial class DocumentsPage : Page
    {
        // ── Режим ─────────────────────────────────────────────────────────
        private enum Mode { Personal, General }
        private Mode _mode = Mode.Personal;

        // ── Данные студентов ───────────────────────────────────────────────
        private List<DocStudentItem> _allStudents = new List<DocStudentItem>();
        private DocStudentItem       _selectedStudent;

        // ── Доступ куратора ────────────────────────────────────────────────
        private bool     _hasAccess;
        private DateTime? _accessExpiry;
        private string   _accessStatus;   // Pending | Approved | Denied | None

        public DocumentsPage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Кнопка запросов — только для Админа
            if (SessionHelper.IsAdmin)
                BtnPendingRequests.Visibility = Visibility.Visible;

            // Кнопка загрузки общих документов — только для Админа
            if (SessionHelper.IsAdmin)
                BtnUploadGeneral.Visibility = Visibility.Visible;

            LoadDocumentNotifications();
            LoadGroups();
            RefreshPendingCount();
        }

        // ── Уведомления куратора ───────────────────────────────────────────

        private void LoadDocumentNotifications()
        {
            PanelNotifs.Children.Clear();
            BannerNotifs.Visibility = Visibility.Collapsed;

            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetDocumentNotifications",
                    new[] { new SqlParameter("@UserId", SessionHelper.UserId) });

                if (dt.Rows.Count == 0) return;

                BannerNotifs.Visibility = Visibility.Visible;
                foreach (DataRow row in dt.Rows)
                {
                    var title = row["Title"]?.ToString()   ?? "";
                    var msg   = row["Message"]?.ToString() ?? "";
                    var at    = row["CreatedAt"] != DBNull.Value
                        ? Convert.ToDateTime(row["CreatedAt"]).ToString("dd.MM.yyyy HH:mm")
                        : "";

                    var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
                    sp.Children.Add(new TextBlock
                    {
                        Text       = "🔔 " + title,
                        FontSize   = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 63, 177))
                    });
                    sp.Children.Add(new TextBlock
                    {
                        Text      = msg + (string.IsNullOrEmpty(at) ? "" : "  ·  " + at),
                        FontSize  = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 74))
                    });
                    PanelNotifs.Children.Add(sp);
                }

                // Пометить как прочитанные
                DatabaseHelper.ExecuteNonQuery("sp_MarkDocumentNotifsRead",
                    new[] { new SqlParameter("@UserId", SessionHelper.UserId) });
            }
            catch { }
        }

        // ── Загрузка групп и студентов ─────────────────────────────────────

        private void LoadGroups()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetAllGroups");
                CmbDocGroup.Items.Clear();

                if (SessionHelper.IsAdmin)
                {
                    var all = new ComboBoxItem { Content = "— Все группы —" };
                    all.Tag = (int?)null;
                    CmbDocGroup.Items.Add(all);
                }

                ComboBoxItem defaultItem = null;
                foreach (DataRow row in dt.Rows)
                {
                    int  gid   = Convert.ToInt32(row["GroupId"]);
                    var  name  = row["GroupName"]?.ToString() ?? "";
                    int? curId = row["CuratorId"] != DBNull.Value
                        ? (int?)Convert.ToInt32(row["CuratorId"]) : null;

                    var item = new ComboBoxItem { Content = name };
                    item.Tag = (int?)gid;
                    CmbDocGroup.Items.Add(item);

                    if (SessionHelper.IsCurator && curId == SessionHelper.UserId)
                        defaultItem = item;
                }

                if (SessionHelper.IsCurator)
                {
                    CmbDocGroup.Items.Clear();
                    var fallback = defaultItem ?? new ComboBoxItem
                        { Content = "— Все группы —", Tag = (int?)null };
                    CmbDocGroup.Items.Add(fallback);
                }

                if (CmbDocGroup.Items.Count > 0)
                    CmbDocGroup.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки групп: " + ex.Message);
            }
        }

        private void CmbDocGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDocGroup == null) return;
            if (CmbDocGroup.SelectedItem is ComboBoxItem item)
                LoadStudents(item.Tag as int?);
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
                    var lastName  = row["LastName"]?.ToString()  ?? "";
                    var firstName = row["FirstName"]?.ToString() ?? "";
                    _allStudents.Add(new DocStudentItem
                    {
                        StudentId = Convert.ToInt32(row["StudentId"]),
                        FullName  = row["FullName"]?.ToString() ?? "",
                        ShortName = string.IsNullOrEmpty(firstName)
                            ? lastName : lastName + " " + firstName[0] + ".",
                        Initial   = lastName.Length > 0 ? lastName[0].ToString() : "?",
                        GroupName = row["GroupName"]?.ToString() ?? "",
                        GroupId   = Convert.ToInt32(row["GroupId"])
                    });
                }

                ApplyStudentFilter();
                TxtPageSubtitle.Text = "Студентов: " + _allStudents.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки студентов: " + ex.Message);
            }
        }

        private void TxtDocSearch_Changed(object sender, TextChangedEventArgs e)
        {
            if (TxtDocSearch == null) return;
            ApplyStudentFilter();
        }

        private void ApplyStudentFilter()
        {
            var search = TxtDocSearch?.Text?.Trim() ?? "";
            var filtered = string.IsNullOrEmpty(search)
                ? _allStudents
                : _allStudents.Where(s =>
                    s.FullName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    s.GroupName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();

            DocStudentList.ItemsSource = filtered;
        }

        private void DocStudentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocStudentList == null) return;
            _selectedStudent = DocStudentList.SelectedItem as DocStudentItem;
            if (_selectedStudent == null) return;

            if (SessionHelper.IsAdmin)
            {
                // Админ всегда имеет доступ
                ShowDocContent(_selectedStudent.FullName, null);
                LoadStudentDocs(_selectedStudent.StudentId);
            }
            else if (SessionHelper.IsCurator)
            {
                CheckAndShowAccess();
            }
        }

        // ── Контроль доступа (куратор) ─────────────────────────────────────

        private void CheckAndShowAccess()
        {
            _hasAccess    = false;
            _accessExpiry = null;
            _accessStatus = "None";

            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetDocumentAccess", new[]
                {
                    new SqlParameter("@CuratorId", SessionHelper.UserId),
                    new SqlParameter("@StudentId", _selectedStudent.StudentId)
                });

                if (dt.Rows.Count > 0)
                {
                    var row   = dt.Rows[0];
                    _accessStatus = row["Status"]?.ToString() ?? "None";
                    _hasAccess    = Convert.ToInt32(row["HasAccess"]) == 1;
                    _accessExpiry = row["ExpiresAt"] != DBNull.Value
                        ? (DateTime?)Convert.ToDateTime(row["ExpiresAt"])
                        : null;
                }
            }
            catch { }

            if (_hasAccess)
            {
                ShowDocContent(_selectedStudent.FullName, _accessExpiry);
                LoadStudentDocs(_selectedStudent.StudentId);
            }
            else
            {
                ShowNoAccess();
            }
        }

        private void ShowDocContent(string studentName, DateTime? expiry)
        {
            DocEmptyState.Visibility = Visibility.Collapsed;
            DocNoAccess.Visibility   = Visibility.Collapsed;
            DocContent.Visibility    = Visibility.Visible;

            TxtDocStudentName.Text = studentName;

            if (expiry.HasValue)
            {
                BdrAccessBadge.Visibility = Visibility.Visible;
                TxtAccessExpiry.Text      = "Доступ до " + expiry.Value.ToString("dd.MM.yyyy");
            }
            else
            {
                BdrAccessBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowNoAccess()
        {
            DocEmptyState.Visibility = Visibility.Collapsed;
            DocContent.Visibility    = Visibility.Collapsed;
            DocNoAccess.Visibility   = Visibility.Visible;

            switch (_accessStatus)
            {
                case "Pending":
                    TxtNoAccessTitle.Text   = "Запрос ожидает одобрения";
                    TxtNoAccessSub.Text     = "Ваш запрос на доступ к документам студента отправлен и ожидает рассмотрения администратором.";
                    BtnRequestAccess.Content   = "Запрос уже отправлен";
                    BtnRequestAccess.IsEnabled = false;
                    break;
                case "Denied":
                    TxtNoAccessTitle.Text   = "В доступе отказано";
                    TxtNoAccessSub.Text     = "Администратор отклонил ваш запрос. Вы можете подать новый запрос.";
                    BtnRequestAccess.Content   = "Запросить доступ снова";
                    BtnRequestAccess.IsEnabled = true;
                    break;
                case "Approved":
                    // Approved но срок истёк
                    TxtNoAccessTitle.Text   = "Срок доступа истёк";
                    TxtNoAccessSub.Text     = "Разрешённый период доступа к документам этого студента закончился.";
                    BtnRequestAccess.Content   = "Запросить доступ снова";
                    BtnRequestAccess.IsEnabled = true;
                    break;
                default:
                    TxtNoAccessTitle.Text   = "Доступ закрыт";
                    TxtNoAccessSub.Text     = "Для просмотра личных документов студента необходимо разрешение администратора.";
                    BtnRequestAccess.Content   = "Запросить доступ";
                    BtnRequestAccess.IsEnabled = true;
                    break;
            }
        }

        private void BtnRequestAccess_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null) return;
            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_RequestDocumentAccess", new[]
                {
                    new SqlParameter("@CuratorId", SessionHelper.UserId),
                    new SqlParameter("@StudentId", _selectedStudent.StudentId)
                });

                _accessStatus              = "Pending";
                TxtNoAccessTitle.Text      = "Запрос ожидает одобрения";
                TxtNoAccessSub.Text        = "Ваш запрос отправлен и ожидает рассмотрения администратором.";
                BtnRequestAccess.Content   = "Запрос уже отправлен";
                BtnRequestAccess.IsEnabled = false;

                RefreshPendingCount();
                MessageBox.Show("Запрос на доступ отправлен администратору.", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Документы студента ─────────────────────────────────────────────

        private void LoadStudentDocs(int studentId)
        {
            PanelDocs.Children.Clear();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetStudentPersonalDocs", new[]
                {
                    new SqlParameter("@StudentId",    studentId),
                    new SqlParameter("@ViewerUserId", SessionHelper.UserId)
                });

                if (dt.Rows.Count == 0)
                {
                    PanelDocs.Children.Add(MakeEmptyNote("📭", "Документов пока нет"));
                    return;
                }

                foreach (DataRow row in dt.Rows)
                    PanelDocs.Children.Add(BuildDocCard(row, isPersonal: true));
            }
            catch (Exception ex)
            {
                PanelDocs.Children.Add(new TextBlock
                {
                    Text = "Ошибка: " + ex.Message,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize   = 12
                });
            }
        }

        private void BtnUploadDoc_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null) return;

            var dlg = new OpenFileDialog
            {
                Title  = "Выберите файл для загрузки",
                Filter = "Все файлы (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            var fi       = new FileInfo(dlg.FileName);
            var fileData = File.ReadAllBytes(dlg.FileName);
            int sizeKB   = (int)(fi.Length / 1024);
            var mime     = GetMimeType(fi.Extension);

            // Диалог ввода названия/типа
            var titleDlg = new DocUploadDialog(fi.Name)
                { Owner = Window.GetWindow(this) };
            if (titleDlg.ShowDialog() != true) return;

            try
            {
                var param = new SqlParameter("@FileData", System.Data.SqlDbType.VarBinary)
                    { Value = fileData };

                DatabaseHelper.ExecuteNonQuery("sp_AddStudentPersonalDoc", new[]
                {
                    new SqlParameter("@StudentId",    _selectedStudent.StudentId),
                    new SqlParameter("@Title",        titleDlg.DocTitle),
                    new SqlParameter("@DocType",      titleDlg.DocType),
                    new SqlParameter("@FileName",     fi.Name),
                    param,
                    new SqlParameter("@MimeType",     mime),
                    new SqlParameter("@FileSizeKB",   sizeKB),
                    new SqlParameter("@Description",  (object)titleDlg.Description ?? DBNull.Value),
                    new SqlParameter("@UploadedById", SessionHelper.UserId)
                });

                LoadStudentDocs(_selectedStudent.StudentId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Общие документы ────────────────────────────────────────────────

        private void LoadGeneralDocs()
        {
            PanelGeneral2.Children.Clear();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetGeneralDocuments");

                TxtGeneralCount.Text = dt.Rows.Count > 0
                    ? "Документов: " + dt.Rows.Count
                    : "Нет общих документов";

                if (dt.Rows.Count == 0)
                {
                    PanelGeneral2.Children.Add(MakeEmptyNote("📋", "Общих документов нет"));
                    return;
                }

                foreach (DataRow row in dt.Rows)
                    PanelGeneral2.Children.Add(BuildDocCard(row, isPersonal: false));
            }
            catch (Exception ex)
            {
                PanelGeneral2.Children.Add(new TextBlock
                {
                    Text      = "Ошибка: " + ex.Message,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize  = 12
                });
            }
        }

        private void BtnUploadGeneral_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Выберите общий документ",
                Filter = "Все файлы (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            var fi       = new FileInfo(dlg.FileName);
            var fileData = File.ReadAllBytes(dlg.FileName);
            int sizeKB   = (int)(fi.Length / 1024);
            var mime     = GetMimeType(fi.Extension);

            var titleDlg = new DocUploadDialog(fi.Name)
                { Owner = Window.GetWindow(this) };
            if (titleDlg.ShowDialog() != true) return;

            try
            {
                var param = new SqlParameter("@FileData", System.Data.SqlDbType.VarBinary)
                    { Value = fileData };

                DatabaseHelper.ExecuteNonQuery("sp_AddGeneralDocument", new[]
                {
                    new SqlParameter("@Title",        titleDlg.DocTitle),
                    new SqlParameter("@DocType",      titleDlg.DocType),
                    new SqlParameter("@FileName",     fi.Name),
                    param,
                    new SqlParameter("@MimeType",     mime),
                    new SqlParameter("@FileSizeKB",   sizeKB),
                    new SqlParameter("@Description",  (object)titleDlg.Description ?? DBNull.Value),
                    new SqlParameter("@UploadedById", SessionHelper.UserId)
                });

                LoadGeneralDocs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Карточка документа ─────────────────────────────────────────────

        private UIElement BuildDocCard(DataRow row, bool isPersonal)
        {
            int    docId      = Convert.ToInt32(row["DocId"]);
            string title      = row["Title"]?.ToString()          ?? "";
            string docType    = row["DocType"]?.ToString()        ?? "";
            string fileName   = row["FileName"]?.ToString()       ?? "";
            string upBy       = row["UploadedByName"]?.ToString() ?? "—";
            int    sizeKB     = row["FileSizeKB"] != DBNull.Value ? Convert.ToInt32(row["FileSizeKB"]) : 0;
            string desc       = row["Description"]?.ToString()    ?? "";
            string icon       = GetFileIcon(fileName);
            DateTime? upAt    = row["UploadedAt"] != DBNull.Value
                ? (DateTime?)Convert.ToDateTime(row["UploadedAt"]) : null;

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

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Иконка
            var iconBdr = new Border
            {
                Width = 44, Height = 44, CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 252)),
                Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center
            };
            iconBdr.Child = new TextBlock
            {
                Text                = icon,
                FontSize            = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBdr, 0);
            mainGrid.Children.Add(iconBdr);

            // Информация
            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text         = title,
                FontSize     = 13,
                FontWeight   = FontWeights.SemiBold,
                Foreground   = new SolidColorBrush(Color.FromRgb(27, 42, 74)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };

            if (!string.IsNullOrEmpty(docType))
                metaRow.Children.Add(MakeTag(docType));

            metaRow.Children.Add(new TextBlock
            {
                Text      = fileName + (sizeKB > 0 ? "  ·  " + FormatSize(sizeKB) : ""),
                FontSize  = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin    = new Thickness(0, 0, 0, 0)
            });
            info.Children.Add(metaRow);

            var upRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            upRow.Children.Add(new TextBlock
            {
                Text      = upBy + (upAt.HasValue ? "  ·  " + upAt.Value.ToString("dd.MM.yyyy") : ""),
                FontSize  = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166))
            });
            info.Children.Add(upRow);

            if (!string.IsNullOrEmpty(desc))
                info.Children.Add(new TextBlock
                {
                    Text         = desc,
                    FontSize     = 11,
                    Foreground   = new SolidColorBrush(Color.FromRgb(90, 102, 122)),
                    Margin       = new Thickness(0, 3, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });

            Grid.SetColumn(info, 1);
            mainGrid.Children.Add(info);

            // Кнопки
            var btnPanel = new StackPanel
            {
                Orientation       = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(12, 0, 0, 0)
            };

            // Скачать
            var btnDownload = MakeIconBtn("⬇️", "#EFF6FC");
            btnDownload.ToolTip = "Скачать";
            btnDownload.Click += (s, ev) => DownloadDoc(docId, fileName, isPersonal);
            btnPanel.Children.Add(btnDownload);

            // Удалить (куратор и Админ)
            if (SessionHelper.IsAdmin || SessionHelper.IsCurator)
            {
                var btnDelete = MakeIconBtn("🗑️", "#FDE7E9");
                btnDelete.ToolTip = "Удалить";
                btnDelete.Click  += (s, ev) =>
                {
                    var res = MessageBox.Show("Удалить документ «" + title + "»?",
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res != MessageBoxResult.Yes) return;
                    try
                    {
                        string sp = isPersonal ? "sp_DeleteStudentPersonalDoc" : "sp_DeleteGeneralDocument";
                        string p  = isPersonal ? "@DocId" : "@DocId";
                        DatabaseHelper.ExecuteNonQuery(sp, new[]
                        {
                            new SqlParameter(p,             docId),
                            new SqlParameter("@DeletedById", SessionHelper.UserId)
                        });
                        if (isPersonal && _selectedStudent != null)
                            LoadStudentDocs(_selectedStudent.StudentId);
                        else
                            LoadGeneralDocs();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                btnPanel.Children.Add(btnDelete);
            }

            Grid.SetColumn(btnPanel, 2);
            mainGrid.Children.Add(btnPanel);

            border.Child = mainGrid;
            return border;
        }

        private void DownloadDoc(int docId, string fileName, bool isPersonal)
        {
            try
            {
                string sp = isPersonal ? "sp_DownloadStudentDoc" : "sp_DownloadGeneralDoc";
                var dt = DatabaseHelper.ExecuteProcedure(sp, new[]
                {
                    new SqlParameter("@DocId",        docId),
                    new SqlParameter("@ViewerUserId", SessionHelper.UserId)
                });

                if (dt.Rows.Count == 0) return;
                byte[] data = dt.Rows[0]["FileData"] as byte[];
                if (data == null) return;

                var saveDlg = new SaveFileDialog
                {
                    FileName = fileName,
                    Title    = "Сохранить документ"
                };
                if (saveDlg.ShowDialog() != true) return;
                File.WriteAllBytes(saveDlg.FileName, data);

                // Открыть файл после сохранения
                try { System.Diagnostics.Process.Start(saveDlg.FileName); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка скачивания: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Переключение режима ────────────────────────────────────────────

        private void BtnModePersonal_Click(object sender, RoutedEventArgs e)
            => SetMode(Mode.Personal);

        private void BtnModeGeneral_Click(object sender, RoutedEventArgs e)
        {
            SetMode(Mode.General);
            LoadGeneralDocs();
        }

        private void SetMode(Mode mode)
        {
            _mode = mode;
            PanelPersonal.Visibility = mode == Mode.Personal ? Visibility.Visible : Visibility.Collapsed;
            PanelGeneral.Visibility  = mode == Mode.General  ? Visibility.Visible : Visibility.Collapsed;

            // Стиль кнопок режима
            UpdateModeButtonStyle(BtnModePersonal, mode == Mode.Personal);
            UpdateModeButtonStyle(BtnModeGeneral,  mode == Mode.General);
        }

        private void UpdateModeButtonStyle(Button btn, bool active)
        {
            // Находим Border внутри ControlTemplate
            btn.ApplyTemplate();
            var bdr = btn.Template.FindName("Bdr", btn) as Border;
            if (bdr == null) return;

            if (active)
            {
                bdr.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                if (bdr.Child is TextBlock tb)
                {
                    tb.FontWeight = FontWeights.SemiBold;
                    tb.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                }
            }
            else
            {
                bdr.BorderBrush = Brushes.Transparent;
                if (bdr.Child is TextBlock tb)
                {
                    tb.FontWeight = FontWeights.Normal;
                    tb.Foreground = new SolidColorBrush(Color.FromRgb(90, 102, 122));
                }
            }
        }

        // ── Запросы администратора ─────────────────────────────────────────

        private void BtnPendingRequests_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AccessRequestsDialog { Owner = Window.GetWindow(this) };
            dlg.ShowDialog();
            if (dlg.AnyChanges)
                RefreshPendingCount();
        }

        private void RefreshPendingCount()
        {
            if (!SessionHelper.IsAdmin) return;
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetPendingAccessRequests");
                int cnt = dt.Rows.Count;
                if (cnt > 0)
                {
                    BtnPendingRequests.Visibility = Visibility.Visible;
                    var tb = BtnPendingRequests.Template.FindName("TxtPendingCount", BtnPendingRequests) as TextBlock;
                    if (tb != null) tb.Text = cnt + " " + Pluralize(cnt, "запрос", "запроса", "запросов");
                }
                else
                {
                    BtnPendingRequests.Visibility = Visibility.Collapsed;
                }
            }
            catch { }
        }

        // ── Вспомогательные ───────────────────────────────────────────────

        private static DropShadowEffect MakeShadow() => new DropShadowEffect
        {
            Color = Color.FromRgb(192, 202, 222),
            BlurRadius = 8, ShadowDepth = 1, Opacity = 0.10, Direction = 270
        };

        private static UIElement MakeEmptyNote(string icon, string msg)
        {
            var sp = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 32, 0, 0)
            };
            sp.Children.Add(new TextBlock
            {
                Text = icon, FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });
            sp.Children.Add(new TextBlock
            {
                Text = msg, FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return sp;
        }

        private static Border MakeTag(string text)
        {
            var b = new Border
            {
                Background   = new SolidColorBrush(Color.FromRgb(239, 246, 252)),
                CornerRadius = new CornerRadius(8),
                Padding      = new Thickness(6, 1, 6, 1),
                Margin       = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            b.Child = new TextBlock
            {
                Text      = text, FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 99, 177))
            };
            return b;
        }

        private static Button MakeIconBtn(string emoji, string bgHex)
        {
            var bg  = (Color)ColorConverter.ConvertFromString(bgHex);
            var btn = new Button { Width = 30, Height = 30, Margin = new Thickness(4, 0, 0, 0),
                                   Cursor = System.Windows.Input.Cursors.Hand };
            var t   = new ControlTemplate(typeof(Button));
            var b   = new FrameworkElementFactory(typeof(Border));
            b.SetValue(Border.BackgroundProperty,   new SolidColorBrush(bg));
            b.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            var tb  = new FrameworkElementFactory(typeof(TextBlock));
            tb.SetValue(TextBlock.TextProperty,               emoji);
            tb.SetValue(TextBlock.FontSizeProperty,           13.0);
            tb.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            tb.SetValue(TextBlock.VerticalAlignmentProperty,   VerticalAlignment.Center);
            b.AppendChild(tb);
            t.VisualTree = b;
            btn.Template = t;
            return btn;
        }

        private static string GetFileIcon(string fileName)
        {
            var ext = Path.GetExtension(fileName ?? "").ToLower();
            switch (ext)
            {
                case ".pdf":                        return "📕";
                case ".doc": case ".docx":          return "📝";
                case ".xls": case ".xlsx":          return "📊";
                case ".jpg": case ".jpeg": case ".png": case ".bmp": return "🖼️";
                case ".zip": case ".rar": case ".7z": return "🗜️";
                default:                            return "📄";
            }
        }

        private static string GetMimeType(string ext)
        {
            switch (ext.ToLower())
            {
                case ".pdf":  return "application/pdf";
                case ".doc":  return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls":  return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".png":  return "image/png";
                case ".zip":  return "application/zip";
                default:      return "application/octet-stream";
            }
        }

        private static string FormatSize(int kb)
        {
            if (kb < 1024) return kb + " КБ";
            return (kb / 1024.0).ToString("F1") + " МБ";
        }

        private static string Pluralize(int n, string one, string few, string many)
        {
            int mod10 = n % 10, mod100 = n % 100;
            if (mod10 == 1 && mod100 != 11)      return one;
            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return few;
            return many;
        }
    }

    // ── Модели ────────────────────────────────────────────────────────────────

    public class DocStudentItem
    {
        public int    StudentId { get; set; }
        public string FullName  { get; set; }
        public string ShortName { get; set; }
        public string Initial   { get; set; }
        public string GroupName { get; set; }
        public int    GroupId   { get; set; }
    }
}
