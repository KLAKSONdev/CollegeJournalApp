using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Views.Dialogs
{
    public partial class AccessRequestsDialog : Window
    {
        public bool AnyChanges { get; private set; }

        private class RequestItem
        {
            public int    RequestId    { get; set; }
            public int    CuratorId   { get; set; }
            public string CuratorName { get; set; }
            public int    StudentId   { get; set; }
            public string StudentName { get; set; }
            public string GroupName   { get; set; }
            public DateTime RequestedAt { get; set; }
        }

        public AccessRequestsDialog()
        {
            InitializeComponent();
            LoadRequests();
        }

        private void LoadRequests()
        {
            PanelRequests.Children.Clear();
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetPendingAccessRequests");
                var items = new List<RequestItem>();

                foreach (DataRow row in dt.Rows)
                {
                    items.Add(new RequestItem
                    {
                        RequestId   = Convert.ToInt32(row["RequestId"]),
                        CuratorId   = Convert.ToInt32(row["CuratorId"]),
                        CuratorName = row["CuratorName"]?.ToString() ?? "",
                        StudentId   = Convert.ToInt32(row["StudentId"]),
                        StudentName = row["StudentName"]?.ToString() ?? "",
                        GroupName   = row["GroupName"]?.ToString()   ?? "",
                        RequestedAt = row["RequestedAt"] != DBNull.Value
                            ? Convert.ToDateTime(row["RequestedAt"])
                            : DateTime.Now
                    });
                }

                TxtRequestCount.Text = items.Count > 0
                    ? "Ожидают рассмотрения: " + items.Count
                    : "Нет ожидающих запросов";

                if (items.Count == 0)
                {
                    PanelRequests.Children.Add(new TextBlock
                    {
                        Text                = "✅  Все запросы обработаны",
                        FontSize            = 14,
                        Foreground          = new SolidColorBrush(Color.FromRgb(16, 124, 16)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin              = new Thickness(0, 40, 0, 0)
                    });
                    return;
                }

                foreach (var req in items)
                    PanelRequests.Children.Add(BuildRequestCard(req));
            }
            catch (Exception ex)
            {
                PanelRequests.Children.Add(new TextBlock
                {
                    Text      = "Ошибка загрузки: " + ex.Message,
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize  = 12
                });
            }
        }

        private UIElement BuildRequestCard(RequestItem req)
        {
            var border = new Border
            {
                Background      = new SolidColorBrush(Colors.White),
                CornerRadius    = new CornerRadius(10),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(227, 230, 238)),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(0, 0, 0, 10),
                Padding         = new Thickness(16, 14, 16, 14)
            };
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(192, 202, 222),
                BlurRadius = 8, ShadowDepth = 1, Opacity = 0.10, Direction = 270
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Информация о запросе
            var info = new StackPanel();
            info.Children.Add(new TextBlock
            {
                Text       = "Куратор: " + req.CuratorName,
                FontSize   = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 74)),
                Margin     = new Thickness(0, 0, 0, 4)
            });
            info.Children.Add(new TextBlock
            {
                Text      = "Студент: " + req.StudentName + " · " + req.GroupName,
                FontSize  = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(90, 102, 122)),
                Margin    = new Thickness(0, 0, 0, 4)
            });
            info.Children.Add(new TextBlock
            {
                Text      = "Запрошено: " + req.RequestedAt.ToString("dd.MM.yyyy HH:mm"),
                FontSize  = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166))
            });
            Grid.SetColumn(info, 0);
            grid.Children.Add(info);

            // Кнопки + DatePicker
            var actionPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(16, 0, 0, 0)
            };

            // Выбор даты
            var dateRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 0, 0, 8)
            };
            dateRow.Children.Add(new TextBlock
            {
                Text              = "До:",
                FontSize          = 11,
                Foreground        = new SolidColorBrush(Color.FromRgb(90, 102, 122)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 8, 0)
            });
            var dtPicker = new DatePicker
            {
                SelectedDate = DateTime.Today.AddDays(7),
                Width        = 130,
                FontSize     = 12
            };
            dateRow.Children.Add(dtPicker);
            actionPanel.Children.Add(dateRow);

            // Кнопки Одобрить / Отказать
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal };

            var btnApprove = MakeButton("✅  Одобрить", "#107C10", Colors.White);
            btnApprove.Margin = new Thickness(0, 0, 8, 0);
            btnApprove.Click += (s, e) =>
            {
                if (!dtPicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату окончания доступа.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (dtPicker.SelectedDate.Value.Date <= DateTime.Today)
                {
                    MessageBox.Show("Дата окончания должна быть в будущем.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_ApproveDocumentAccess", new[]
                    {
                        new SqlParameter("@RequestId", req.RequestId),
                        new SqlParameter("@AdminId",   SessionHelper.UserId),
                        new SqlParameter("@ExpiresAt", dtPicker.SelectedDate.Value)
                    });
                    AnyChanges = true;
                    LoadRequests();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            var btnDeny = MakeButton("❌  Отказать", "#C43E1C", Colors.White);
            btnDeny.Click += (s, e) =>
            {
                var res = MessageBox.Show(
                    "Отказать в доступе куратору " + req.CuratorName + "?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes) return;
                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_DenyDocumentAccess", new[]
                    {
                        new SqlParameter("@RequestId", req.RequestId),
                        new SqlParameter("@AdminId",   SessionHelper.UserId)
                    });
                    AnyChanges = true;
                    LoadRequests();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            btnRow.Children.Add(btnApprove);
            btnRow.Children.Add(btnDeny);
            actionPanel.Children.Add(btnRow);

            Grid.SetColumn(actionPanel, 1);
            grid.Children.Add(actionPanel);

            border.Child = grid;
            return border;
        }

        private Button MakeButton(string text, string bgHex, Color fg)
        {
            var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
            var btn = new Button
            {
                Height = 30,
                Padding = new Thickness(12, 0, 12, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var tmpl   = new ControlTemplate(typeof(Button));
            var bdrFac = new FrameworkElementFactory(typeof(Border));
            bdrFac.SetValue(Border.BackgroundProperty,   new SolidColorBrush(bgColor));
            bdrFac.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            bdrFac.SetValue(Border.PaddingProperty,      new Thickness(12, 0, 12, 0));
            var tbFac  = new FrameworkElementFactory(typeof(TextBlock));
            tbFac.SetValue(TextBlock.TextProperty,               text);
            tbFac.SetValue(TextBlock.FontSizeProperty,           12.0);
            tbFac.SetValue(TextBlock.ForegroundProperty,         new SolidColorBrush(fg));
            tbFac.SetValue(TextBlock.VerticalAlignmentProperty,  VerticalAlignment.Center);
            tbFac.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            bdrFac.AppendChild(tbFac);
            tmpl.VisualTree = bdrFac;
            btn.Template    = tmpl;
            return btn;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
