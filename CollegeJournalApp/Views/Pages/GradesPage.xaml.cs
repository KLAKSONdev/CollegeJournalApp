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
    public partial class GradesPage : Page
    {
        private List<GradeRow> _all = new List<GradeRow>();
        private DataGrid _grid;
        private TextBox _search;
        private ComboBox _cmbSubject, _cmbGrade;
        private TextBlock _statAvg, _statFive, _statTwo, _statCount, _total;

        public GradesPage()
        {
            InitializeComponent();
            KeepAlive = false;
            Loaded += (s, e) => Init();
        }

        private void Init()
        {
            _grid       = FindName("GradesGrid") as DataGrid;
            _search     = FindName("TxtSearch")  as TextBox;
            _cmbSubject = FindName("CmbSubject") as ComboBox;
            _cmbGrade   = FindName("CmbGrade")   as ComboBox;
            _total      = FindName("TxtTotal")   as TextBlock;
            _statAvg    = FindName("StatAvg")    as TextBlock;
            _statFive   = FindName("StatFive")   as TextBlock;
            _statTwo    = FindName("StatTwo")    as TextBlock;
            _statCount  = FindName("StatCount")  as TextBlock;

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
            var hdr = new Border { Background=Brushes.White,
                BorderBrush=new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness=new Thickness(0,0,0,1), Padding=new Thickness(24,14,24,14) };
            var hp = new StackPanel { Orientation=Orientation.Horizontal };
            hp.Children.Add(new TextBlock { Text="Успеваемость", FontSize=18,
                FontWeight=FontWeights.SemiBold, Foreground=new SolidColorBrush(Color.FromRgb(31,31,31)) });
            _total = new TextBlock { FontSize=12, Foreground=new SolidColorBrush(Color.FromRgb(96,94,92)),
                VerticalAlignment=VerticalAlignment.Bottom, Margin=new Thickness(10,0,0,2) };
            hp.Children.Add(_total); hdr.Child=hp;
            Grid.SetRow(hdr,0); root.Children.Add(hdr);

            // Статистика
            var stats = new System.Windows.Controls.Primitives.UniformGrid { Columns = 4, Margin = new Thickness(24, 16, 24, 0) }; _statAvg   = AddStat(stats,"Средний балл",  Color.FromRgb(0,120,212));
            _statFive  = AddStat(stats,"Отличников (5)",Color.FromRgb(16,124,16));
            _statTwo   = AddStat(stats,"Двоечников (2)",Color.FromRgb(209,52,56));
            _statCount = AddStat(stats,"Всего оценок",  Color.FromRgb(50,49,48));
            Grid.SetRow(stats,1); root.Children.Add(stats);

            // Тулбар
            var tb = new Border { Background=Brushes.White,
                BorderBrush=new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness=new Thickness(0,1,0,1), Padding=new Thickness(24,10,24,10),
                Margin=new Thickness(0,12,0,0) };
            var tp = new StackPanel { Orientation=Orientation.Horizontal };

            _search = new TextBox { Width=200, Height=30, Padding=new Thickness(8,6,8,6), FontSize=12,
                BorderBrush=new SolidColorBrush(Color.FromRgb(208,208,208)) };
            _search.TextChanged += (s,e) => ApplyFilter();
            tp.Children.Add(_search);

            _cmbSubject = new ComboBox { Width=200, Height=30, Margin=new Thickness(8,0,0,0), FontSize=12 };
            _cmbSubject.SelectionChanged += (s,e) => ApplyFilter();
            tp.Children.Add(_cmbSubject);

            _cmbGrade = new ComboBox { Width=130, Height=30, Margin=new Thickness(8,0,0,0), FontSize=12 };
            foreach (var g in new[]{"Все оценки","5","4","3","2"})
                _cmbGrade.Items.Add(new ComboBoxItem { Content=g });
            _cmbGrade.SelectedIndex=0;
            _cmbGrade.SelectionChanged += (s,e) => ApplyFilter();
            tp.Children.Add(_cmbGrade);

            var btnR = new Button { Content="Сбросить", Height=30, Padding=new Thickness(12,0,12,0),
                Margin=new Thickness(8,0,0,0), Background=Brushes.Transparent,
                BorderBrush=new SolidColorBrush(Color.FromRgb(208,208,208)), FontSize=12,
                Cursor=System.Windows.Input.Cursors.Hand };
            btnR.Click += (s,e) => { _search.Text=""; _cmbSubject.SelectedIndex=0; _cmbGrade.SelectedIndex=0; };
            tp.Children.Add(btnR);

            var btnE = new Button { Content="📥 Экспорт", Height=30, Padding=new Thickness(12,0,12,0),
                Margin=new Thickness(8,0,0,0), Background=new SolidColorBrush(Color.FromRgb(16,124,16)),
                Foreground=Brushes.White, BorderThickness=new Thickness(0), FontSize=12,
                Cursor=System.Windows.Input.Cursors.Hand };
            btnE.Click += BtnExport_Click;
            tp.Children.Add(btnE);

            tb.Child=tp;
            Grid.SetRow(tb,2); root.Children.Add(tb);

            // Таблица
            _grid = new DataGrid { AutoGenerateColumns=false, IsReadOnly=true,
                GridLinesVisibility=DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush=new SolidColorBrush(Color.FromRgb(243,242,241)),
                BorderThickness=new Thickness(0), Background=Brushes.White, RowBackground=Brushes.White,
                AlternatingRowBackground=new SolidColorBrush(Color.FromRgb(250,250,250)),
                HeadersVisibility=DataGridHeadersVisibility.Column, CanUserResizeRows=false,
                FontFamily=new FontFamily("Segoe UI"), FontSize=12, RowHeight=34 };
            _grid.Columns.Add(new DataGridTextColumn { Header="Студент",    Binding=new System.Windows.Data.Binding("StudentName"), Width=new DataGridLength(220) });
            _grid.Columns.Add(new DataGridTextColumn { Header="Дисциплина", Binding=new System.Windows.Data.Binding("Subject"),     Width=new DataGridLength(1,DataGridLengthUnitType.Star) });
            _grid.Columns.Add(new DataGridTextColumn { Header="Тип",        Binding=new System.Windows.Data.Binding("GradeType"),   Width=new DataGridLength(130) });
            _grid.Columns.Add(new DataGridTextColumn { Header="Оценка",     Binding=new System.Windows.Data.Binding("GradeValue"),  Width=new DataGridLength(80) });
            _grid.Columns.Add(new DataGridTextColumn { Header="Дата",       Binding=new System.Windows.Data.Binding("GradeDate"),   Width=new DataGridLength(100) });

            var brd = new Border { Margin=new Thickness(24,12,24,16),
                BorderBrush=new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness=new Thickness(1), Child=_grid };
            Grid.SetRow(brd,3); root.Children.Add(brd);
            this.Content=root;
        }

        private TextBlock AddStat(Panel p, string label, Color color)
        {
            var b = new Border { Background=Brushes.White,
                BorderBrush=new SolidColorBrush(Color.FromRgb(224,224,224)),
                BorderThickness=new Thickness(1), Margin=new Thickness(0,0,8,0) };
            var sp = new StackPanel { Margin=new Thickness(16,12,16,12) };
            var val = new TextBlock { Text="—", FontSize=24, FontWeight=FontWeights.SemiBold,
                Foreground=new SolidColorBrush(color) };
            sp.Children.Add(val);
            sp.Children.Add(new TextBlock { Text=label, FontSize=10,
                Foreground=new SolidColorBrush(Color.FromRgb(96,94,92)), Margin=new Thickness(0,3,0,0) });
            b.Child=sp; p.Children.Add(b); return val;
        }

        private void LoadData()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteProcedure("sp_GetGroupGrades", new[]
                {
                    new SqlParameter("@UserId",   SessionHelper.UserId),
                    new SqlParameter("@RoleName", SessionHelper.RoleName)
                });
                _all.Clear();
                var subjects = new HashSet<string>();
                foreach (DataRow r in dt.Rows)
                {
                    var subj = r["SubjectName"]?.ToString() ?? "—";
                    subjects.Add(subj);
                    _all.Add(new GradeRow
                    {
                        StudentName = r["StudentName"]?.ToString() ?? "—",
                        Subject     = subj,
                        GradeType   = r["GradeType"]?.ToString()   ?? "—",
                        GradeValue  = r["GradeValue"]?.ToString()  ?? "—",
                        GradeDate   = r["GradeDate"] != DBNull.Value ? Convert.ToDateTime(r["GradeDate"]).ToString("dd.MM.yyyy") : "—"
                    });
                }
                if (_cmbSubject != null)
                {
                    _cmbSubject.Items.Clear();
                    _cmbSubject.Items.Add(new ComboBoxItem { Content="Все дисциплины", IsSelected=true });
                    foreach (var s in subjects.OrderBy(x=>x))
                        _cmbSubject.Items.Add(new ComboBoxItem { Content=s });
                    _cmbSubject.SelectedIndex=0;
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:\n"+ex.Message,"Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (_grid==null) return;
            var f = _all.AsEnumerable();
            var q = _search?.Text?.Trim().ToLower()??"";
            if (!string.IsNullOrEmpty(q)) f=f.Where(r=>r.StudentName.ToLower().Contains(q));
            if (_cmbSubject?.SelectedIndex>0){var s=(_cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString()??"";f=f.Where(r=>r.Subject==s);}
            if (_cmbGrade?.SelectedIndex>0){var g=(_cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString()??"";f=f.Where(r=>r.GradeValue==g);}
            var result=f.ToList();
            _grid.ItemsSource=result;
            if(_total!=null)_total.Text=$"— {result.Count} оценок";
            var vals=result.Where(r=>int.TryParse(r.GradeValue,out _)).Select(r=>int.Parse(r.GradeValue)).ToList();
            if(_statAvg  !=null)_statAvg.Text  =vals.Count>0?$"{vals.Average():F1}":"—";
            if(_statFive !=null)_statFive.Text  =vals.Count(v=>v==5).ToString();
            if(_statTwo  !=null)_statTwo.Text   =vals.Count(v=>v==2).ToString();
            if(_statCount!=null)_statCount.Text =result.Count.ToString();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        { if(_search!=null)_search.Text=""; if(_cmbSubject!=null)_cmbSubject.SelectedIndex=0; if(_cmbGrade!=null)_cmbGrade.SelectedIndex=0; }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var src=_grid?.ItemsSource as List<GradeRow>;
            if(src==null||src.Count==0){MessageBox.Show("Нет данных.","Экспорт",MessageBoxButton.OK,MessageBoxImage.Information);return;}
            var dlg=new SaveFileDialog{Title="Сохранить успеваемость",Filter="Excel|*.xlsx",FileName=$"Успеваемость_{DateTime.Now:yyyy-MM-dd}"};
            if(dlg.ShowDialog()!=true)return;
            try
            {
                using(var wb=new XLWorkbook())
                {
                    var ws=wb.Worksheets.Add("Успеваемость");
                    ws.Style.Font.FontName="Arial";ws.Style.Font.FontSize=10;
                    ws.Cell(1,1).Value="Успеваемость";ws.Cell(1,1).Style.Font.Bold=true;ws.Cell(1,1).Style.Font.FontSize=14;
                    ws.Cell(2,1).Value=$"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}";ws.Cell(2,1).Style.Font.FontColor=XLColor.Gray;
                    string[]h={"Студент","Дисциплина","Тип","Оценка","Дата"};
                    for(int c=0;c<h.Length;c++){var cell=ws.Cell(4,c+1);cell.Value=h[c];cell.Style.Font.Bold=true;
                        cell.Style.Fill.BackgroundColor=XLColor.FromArgb(0,120,212);cell.Style.Font.FontColor=XLColor.White;}
                    for(int i=0;i<src.Count;i++){
                        ws.Cell(5+i,1).Value=src[i].StudentName;ws.Cell(5+i,2).Value=src[i].Subject;
                        ws.Cell(5+i,3).Value=src[i].GradeType;ws.Cell(5+i,4).Value=src[i].GradeValue;ws.Cell(5+i,5).Value=src[i].GradeDate;
                        if(i%2==1)ws.Range(5+i,1,5+i,5).Style.Fill.BackgroundColor=XLColor.FromArgb(245,245,245);}
                    ws.Column(1).Width=28;ws.Column(2).Width=30;ws.Column(3).Width=20;ws.Column(4).Width=10;ws.Column(5).Width=12;
                    wb.SaveAs(dlg.FileName);
                }
                if(MessageBox.Show("Открыть?","Готово",MessageBoxButton.YesNo,MessageBoxImage.Information)==MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(dlg.FileName);
            }
            catch(Exception ex){MessageBox.Show("Ошибка:\n"+ex.Message,"Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);}
        }
    }

    public class GradeRow
    {
        public string StudentName{get;set;} public string Subject{get;set;}
        public string GradeType{get;set;}   public string GradeValue{get;set;}
        public string GradeDate{get;set;}   public string GradeColor{get;set;}
    }
}
