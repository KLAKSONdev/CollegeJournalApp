using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosedXML.Excel;
using CollegeJournalApp.Database;
using CollegeJournalApp.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace CollegeJournalApp.Views.Pages
{
    public partial class AttendancePage : Page
    {
        private List<AttRow> _all = new List<AttRow>();
        private DataGrid _grid;
        private TextBox _search;
        private ComboBox _cmbStatus, _cmbSubject;
        private TextBlock _statPresent, _statAbsent, _statLate, _statPercent, _total;

        public AttendancePage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => Init();
        }

        private void Init()
        {
            _grid       = FindName("AttGrid")    as DataGrid;
            _search     = FindName("TxtSearch")  as TextBox;
            _cmbStatus  = FindName("CmbStatus")  as ComboBox;
            _cmbSubject = FindName("CmbSubject") as ComboBox;
            _total      = FindName("TxtTotal")   as TextBlock;
            _statPresent= FindName("StatPresent")as TextBlock;
            _statAbsent = FindName("StatAbsent") as TextBlock;
            _statLate   = FindName("StatLate")   as TextBlock;
            _statPercent= FindName("StatPercent")as TextBlock;

            if (_grid == null) BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Заголовок
            var hdr = new Border { Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness = new Thickness(0,0,0,1), Padding = new Thickness(24,14,24,14) };
            var hdrPanel = new StackPanel { Orientation = Orientation.Horizontal };
            hdrPanel.Children.Add(new TextBlock { Text = "Посещаемость", FontSize = 18,
                FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(31,31,31)) });
            _total = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(96,94,92)),
                VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(10,0,0,2) };
            hdrPanel.Children.Add(_total);
            hdr.Child = hdrPanel;
            Grid.SetRow(hdr, 0); root.Children.Add(hdr);

            // Статистика
            var stats = new System.Windows.Controls.Primitives.UniformGrid { Columns = 4, Margin = new Thickness(24, 16, 24, 0) };
            _statPresent = AddStatCard(stats, "Присутствовал", Color.FromRgb(16,124,16));
            _statAbsent  = AddStatCard(stats, "Отсутствовал",  Color.FromRgb(209,52,56));
            _statLate    = AddStatCard(stats, "Опоздал",       Color.FromRgb(202,80,16));
            _statPercent = AddStatCard(stats, "% посещаемости",Color.FromRgb(0,120,212));
            Grid.SetRow(stats, 1); root.Children.Add(stats);

            // Тулбар
            var toolbar = new Border { Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness = new Thickness(0,1,0,1), Padding = new Thickness(24,10,24,10),
                Margin = new Thickness(0,12,0,0) };
            var tp = new StackPanel { Orientation = Orientation.Horizontal };

            _search = new TextBox { Width=200, Height=30, Padding=new Thickness(8,6,8,6), FontSize=12,
                BorderBrush=new SolidColorBrush(Color.FromRgb(208,208,208)) };
            _search.TextChanged += (s,e) => ApplyFilter();
            tp.Children.Add(_search);

            _cmbStatus = new ComboBox { Width=160, Height=30, Margin=new Thickness(8,0,0,0), FontSize=12 };
            _cmbStatus.Items.Add(new ComboBoxItem { Content="Все статусы", IsSelected=true });
            _cmbStatus.Items.Add(new ComboBoxItem { Content="Присутствовал" });
            _cmbStatus.Items.Add(new ComboBoxItem { Content="Отсутствовал" });
            _cmbStatus.Items.Add(new ComboBoxItem { Content="Опоздал" });
            _cmbStatus.Items.Add(new ComboBoxItem { Content="Уважительная причина" });
            _cmbStatus.SelectedIndex = 0;
            _cmbStatus.SelectionChanged += (s,e) => ApplyFilter();
            tp.Children.Add(_cmbStatus);

            _cmbSubject = new ComboBox { Width=200, Height=30, Margin=new Thickness(8,0,0,0), FontSize=12 };
            _cmbSubject.SelectionChanged += (s,e) => ApplyFilter();
            tp.Children.Add(_cmbSubject);

            var btnReset = new Button { Content="Сбросить", Height=30, Padding=new Thickness(12,0,12,0),
                Margin=new Thickness(8,0,0,0), Background=Brushes.Transparent,
                BorderBrush=new SolidColorBrush(Color.FromRgb(208,208,208)), FontSize=12,
                Cursor=System.Windows.Input.Cursors.Hand };
            btnReset.Click += (s,e) => { _search.Text=""; _cmbStatus.SelectedIndex=0; _cmbSubject.SelectedIndex=0; };
            tp.Children.Add(btnReset);

            var btnExport = new Button { Content="📥 Экспорт", Height=30, Padding=new Thickness(12,0,12,0),
                Margin=new Thickness(8,0,0,0), Background=new SolidColorBrush(Color.FromRgb(16,124,16)),
                Foreground=Brushes.White, BorderThickness=new Thickness(0), FontSize=12,
                Cursor=System.Windows.Input.Cursors.Hand };
            btnExport.Click += BtnExport_Click;
            tp.Children.Add(btnExport);

            toolbar.Child = tp;
            Grid.SetRow(toolbar, 2); root.Children.Add(toolbar);

            // Таблица
            _grid = new DataGrid { AutoGenerateColumns=false, IsReadOnly=true,
                GridLinesVisibility=DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush=new SolidColorBrush(Color.FromRgb(243,242,241)),
                BorderThickness=new Thickness(0), Background=Brushes.White,
                RowBackground=Brushes.White,
                AlternatingRowBackground=new SolidColorBrush(Color.FromRgb(250,250,250)),
                HeadersVisibility=DataGridHeadersVisibility.Column, CanUserResizeRows=false,
                FontFamily=new FontFamily("Segoe UI"), FontSize=12, RowHeight=34 };

            _grid.Columns.Add(new DataGridTextColumn { Header="Дата",       Binding=new System.Windows.Data.Binding("LessonDate"),  Width=new DataGridLength(100) });
            _grid.Columns.Add(new DataGridTextColumn { Header="Студент",     Binding=new System.Windows.Data.Binding("StudentName"), Width=new DataGridLength(220) });
            _grid.Columns.Add(new DataGridTextColumn { Header="Дисциплина",  Binding=new System.Windows.Data.Binding("Subject"),     Width=new DataGridLength(1, DataGridLengthUnitType.Star) });
            _grid.Columns.Add(new DataGridTextColumn { Header="Статус",      Binding=new System.Windows.Data.Binding("Status"),      Width=new DataGridLength(150) });
            _grid.Columns.Add(new DataGridTextColumn { Header="Причина",     Binding=new System.Windows.Data.Binding("Reason"),      Width=new DataGridLength(160) });

            var border = new Border { Margin=new Thickness(24,12,24,16),
                BorderBrush=new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness=new Thickness(1), Child=_grid };
            Grid.SetRow(border, 3); root.Children.Add(border);
            this.Content = root;
        }

        private TextBlock AddStatCard(Panel parent, string label, Color color)
        {
            var border = new Border { Background=Brushes.White,
                BorderBrush=new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness=new Thickness(1), Margin=new Thickness(0,0,8,0) };
            var sp = new StackPanel { Margin=new Thickness(16,12,16,12) };
            var val = new TextBlock { Text="—", FontSize=24, FontWeight=FontWeights.SemiBold,
                Foreground=new SolidColorBrush(color) };
            sp.Children.Add(val);
            sp.Children.Add(new TextBlock { Text=label, FontSize=10,
                Foreground=new SolidColorBrush(Color.FromRgb(96,94,92)), Margin=new Thickness(0,3,0,0) });
            border.Child = sp;
            parent.Children.Add(border);
            return val;
        }

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetGroupAttendance", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });

                _all.Clear();
                var subjects = new HashSet<string>();
                foreach (DataRow r in dt.Rows)
                {
                    var status  = r["Status"]?.ToString()      ?? "—";
                    var subject = r["SubjectName"]?.ToString() ?? "—";
                    subjects.Add(subject);
                    _all.Add(new AttRow
                    {
                        LessonDate  = r["LessonDate"] != DBNull.Value ? Convert.ToDateTime(r["LessonDate"]).ToString("dd.MM.yyyy") : "—",
                        StudentName = r["StudentName"]?.ToString() ?? "—",
                        Subject     = subject,
                        Status      = status,
                        Reason      = r["Reason"]?.ToString()  ?? "—",
                        StatusColor = GetStatusColor(status)
                    });
                }

                if (_cmbSubject != null)
                {
                    _cmbSubject.Items.Clear();
                    _cmbSubject.Items.Add(new ComboBoxItem { Content="Все дисциплины", IsSelected=true });
                    foreach (var s in subjects.OrderBy(x => x))
                        _cmbSubject.Items.Add(new ComboBoxItem { Content=s });
                    _cmbSubject.SelectedIndex = 0;
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_grid == null) return;
            var filtered = _all.AsEnumerable();
            var search = _search?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(r => r.StudentName.ToLower().Contains(search));
            if (_cmbStatus?.SelectedIndex > 0)
            {
                var st = (_cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                filtered = filtered.Where(r => r.Status == st);
            }
            if (_cmbSubject?.SelectedIndex > 0)
            {
                var subj = (_cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                filtered = filtered.Where(r => r.Subject == subj);
            }
            var result = filtered.ToList();
            _grid.ItemsSource = result;
            if (_total != null) _total.Text = $"— {result.Count} записей";

            int present = result.Count(r => r.Status == "Присутствовал");
            int absent  = result.Count(r => r.Status == "Отсутствовал");
            int late    = result.Count(r => r.Status == "Опоздал");
            int total   = result.Count;
            if (_statPresent != null) _statPresent.Text = present.ToString();
            if (_statAbsent  != null) _statAbsent.Text  = absent.ToString();
            if (_statLate    != null) _statLate.Text     = late.ToString();
            if (_statPercent != null) _statPercent.Text  = total > 0 ? $"{Math.Round(100.0*present/total,1)}%" : "—";
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        { if(_search!=null)_search.Text=""; if(_cmbStatus!=null)_cmbStatus.SelectedIndex=0; if(_cmbSubject!=null)_cmbSubject.SelectedIndex=0; }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var source = _grid?.ItemsSource as List<AttRow>;
            if (source == null || source.Count == 0)
            { MessageBox.Show("Нет данных.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information); return; }

            var dlg = new SaveFileDialog { Title="Сохранить посещаемость", Filter="Excel|*.xlsx",
                FileName=$"Посещаемость_{DateTime.Now:yyyy-MM-dd}" };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Посещаемость");
                    ws.Style.Font.FontName="Arial"; ws.Style.Font.FontSize=10;
                    ws.Cell(1,1).Value="Посещаемость"; ws.Cell(1,1).Style.Font.Bold=true; ws.Cell(1,1).Style.Font.FontSize=14;
                    ws.Cell(2,1).Value=$"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}"; ws.Cell(2,1).Style.Font.FontColor=XLColor.Gray;
                    string[] h={"Дата","Студент","Дисциплина","Статус","Причина"};
                    for(int c=0;c<h.Length;c++){var cell=ws.Cell(4,c+1);cell.Value=h[c];cell.Style.Font.Bold=true;
                        cell.Style.Fill.BackgroundColor=XLColor.FromArgb(0,120,212);cell.Style.Font.FontColor=XLColor.White;}
                    for(int i=0;i<source.Count;i++){
                        ws.Cell(5+i,1).Value=source[i].LessonDate; ws.Cell(5+i,2).Value=source[i].StudentName;
                        ws.Cell(5+i,3).Value=source[i].Subject;    ws.Cell(5+i,4).Value=source[i].Status;
                        ws.Cell(5+i,5).Value=source[i].Reason;
                        if(i%2==1)ws.Range(5+i,1,5+i,5).Style.Fill.BackgroundColor=XLColor.FromArgb(245,245,245);}
                    ws.Column(1).Width=12; ws.Column(2).Width=28; ws.Column(3).Width=30; ws.Column(4).Width=20; ws.Column(5).Width=25;
                    wb.SaveAs(dlg.FileName);
                }
                if(MessageBox.Show("Открыть файл?","Готово",MessageBoxButton.YesNo,MessageBoxImage.Information)==MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(dlg.FileName);
            }
            catch(Exception ex){MessageBox.Show("Ошибка:\n"+ex.Message,"Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);}
        }

        private string GetStatusColor(string s)
        {
            switch(s){case "Присутствовал":return "#107C10";case "Отсутствовал":return "#D13438";
                case "Опоздал":return "#CA5010";case "Уважительная причина":return "#0078D4";default:return "#A19F9D";}
        }
    }

    public class AttRow
    {
        public string LessonDate{get;set;} public string StudentName{get;set;}
        public string Subject{get;set;}    public string Status{get;set;}
        public string Reason{get;set;}     public string StatusColor{get;set;}
    }
}
