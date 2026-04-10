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

    public class AttendancePage    : Page { public AttendancePage()    { Content = PageHelper.MakePage("Посещаемость");  } }
    public class GradesPage        : Page { public GradesPage()        { Content = PageHelper.MakePage("Успеваемость");  } }
    public class SchedulePage      : Page { public SchedulePage()      { Content = PageHelper.MakePage("Расписание");    } }
    public class EventsPage        : Page { public EventsPage()        { Content = PageHelper.MakePage("События");       } }
    public class AnnouncementsPage : Page { public AnnouncementsPage() { Content = PageHelper.MakePage("Объявления");    } }
    public class AssignmentsPage   : Page { public AssignmentsPage()   { Content = PageHelper.MakePage("Поручения");     } }
    public class DocumentsPage     : Page { public DocumentsPage()     { Content = PageHelper.MakePage("Документы");     } }
    public class SocialPage        : Page { public SocialPage()        { Content = PageHelper.MakePage("Соц. карточки"); } }
    public class AchievementsPage  : Page { public AchievementsPage()  { Content = PageHelper.MakePage("Достижения");    } }
    public class AuditPage         : Page { public AuditPage()         { Content = PageHelper.MakePage("Журнал аудита"); } }
    public class UsersPage         : Page { public UsersPage()         { Content = PageHelper.MakePage("Пользователи");  } }
}
