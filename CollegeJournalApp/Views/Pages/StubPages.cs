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
    public class EventsPage        : Page { public EventsPage()        { Content = PageHelper.MakePage("События");       KeepAlive = false; } }
    public class AnnouncementsPage : Page { public AnnouncementsPage() { Content = PageHelper.MakePage("Объявления");    KeepAlive = false; } }
    public class AssignmentsPage   : Page { public AssignmentsPage()   { Content = PageHelper.MakePage("Поручения");     KeepAlive = false; } }
    public class DocumentsPage     : Page { public DocumentsPage()     { Content = PageHelper.MakePage("Документы");     KeepAlive = false; } }
    public class SocialPage        : Page { public SocialPage()        { Content = PageHelper.MakePage("Соц. карточки"); KeepAlive = false; } }
    public class AchievementsPage  : Page { public AchievementsPage()  { Content = PageHelper.MakePage("Достижения");    KeepAlive = false; } }
    public class UsersPage         : Page { public UsersPage()         { Content = PageHelper.MakePage("Пользователи");  KeepAlive = false; } }
}
