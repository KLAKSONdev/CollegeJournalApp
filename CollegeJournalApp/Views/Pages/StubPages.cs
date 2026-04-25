using System.Windows.Controls;
using System.Windows.Media;

namespace CollegeJournalApp.Views.Pages
{
    internal static class PageHelper
    {
        public static System.Windows.UIElement MakePage(string title)
        {
            return new TextBlock
            {
                Text = title + " — раздел в разработке",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 122, 153)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment   = System.Windows.VerticalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI")
            };
        }
    }

    // AttendancePage, GradesPage, SchedulePage — перенесены в отдельные XAML файлы
    // AnnouncementsPage — перенесена в отдельный XAML файл
    // DocumentsPage — перенесена в отдельный XAML файл
    // PortfolioPage — перенесена в отдельный XAML файл
}
