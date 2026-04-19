using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CollegeJournalApp.Helpers;

namespace CollegeJournalApp.Views.Dialogs
{
    public class HelpWindow : Window
    {
        private static readonly Dictionary<string, int> PageSectionMap =
            new Dictionary<string, int>
            {
                { "DashboardPage",     0 },
                { "StudentsPage",      1 },
                { "AttendancePage",    2 },
                { "GradesPage",        3 },
                { "SchedulePage",      4 },
                { "EventsPage",        5 },
                { "AnnouncementsPage", 6 },
                { "AssignmentsPage",   6 },
                { "DocumentsPage",     6 },
                { "SocialPage",        7 },
                { "AchievementsPage",  7 },
                { "AuditPage",         8 },
                { "UsersPage",         8 },
                { "AdminPage",         8 },
            };

        private readonly string _role;

        private ListBox      _nav;
        private ScrollViewer _content;
        private List<(string Title, UIElement Panel)> _sections;

        public HelpWindow(string currentPageName = null, Window owner = null, string role = null)
        {
            _role = role ?? SessionHelper.RoleName ?? "Admin";

            Title                 = "Справка";
            Width                 = 760;
            Height                = 580;
            MinWidth              = 760;
            MinHeight             = 580;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode            = ResizeMode.CanResizeWithGrip;
            FontFamily            = new FontFamily("Segoe UI");
            Background            = new SolidColorBrush(Color.FromRgb(245, 246, 247));
            Owner                 = owner;

            _sections = BuildAllSections();
            BuildWindow();

            // Переходим на нужный раздел, только если он есть для этой роли
            int idx = 0;
            if (currentPageName != null && PageSectionMap.TryGetValue(currentPageName, out int raw))
            {
                // raw — глобальный индекс; ищем раздел по тексту навигации
                var wanted = GlobalTitle(raw);
                for (int i = 0; i < _sections.Count; i++)
                    if (_sections[i].Title == wanted) { idx = i; break; }
            }
            _nav.SelectedIndex = idx;
        }

        // Глобальные заголовки (для сопоставления с PageSectionMap)
        private static string GlobalTitle(int idx)
        {
            var all = new[]
            {
                "Начало работы", "Студенты", "Посещаемость", "Оценки",
                "Расписание", "Мероприятия", "Объявления", "Соц. работа", "Администрирование"
            };
            return idx >= 0 && idx < all.Length ? all[idx] : "";
        }

        // ── Роль-хелпер ───────────────────────────────────────────────────

        private bool Is(params string[] roles) => roles.Contains(_role);

        // ── UI ────────────────────────────────────────────────────────────

        private void BuildWindow()
        {
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var navBorder = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(250, 250, 251)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(218, 220, 224)),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            _nav = new ListBox
            {
                BorderThickness = new Thickness(0),
                Background      = Brushes.Transparent,
                Padding         = new Thickness(0, 6, 0, 6)
            };
            _nav.ItemContainerStyle = MakeNavItemStyle();

            foreach (var (title, _) in _sections)
                _nav.Items.Add(new TextBlock
                {
                    Text         = title,
                    FontSize     = 12,
                    TextWrapping = TextWrapping.Wrap
                });

            _nav.SelectionChanged += (s, e) =>
            {
                if (_nav.SelectedIndex >= 0 && _nav.SelectedIndex < _sections.Count)
                {
                    _content.Content = _sections[_nav.SelectedIndex].Panel;
                    _content.ScrollToTop();
                }
            };

            navBorder.Child = _nav;
            Grid.SetColumn(navBorder, 0);
            body.Children.Add(navBorder);

            _content = new ScrollViewer
            {
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background                    = new SolidColorBrush(Color.FromRgb(245, 246, 247))
            };
            Grid.SetColumn(_content, 1);
            body.Children.Add(_content);

            Content = body;
        }

        private static Style MakeNavItemStyle()
        {
            var style = new Style(typeof(ListBoxItem));
            style.Setters.Add(new Setter(Control.PaddingProperty,
                new Thickness(16, 8, 16, 8)));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty,
                System.Windows.Input.Cursors.Hand));
            style.Setters.Add(new Setter(Control.BackgroundProperty,
                Brushes.Transparent));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty,
                new Thickness(0)));
            style.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty,
                null));

            var sel = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(Control.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(224, 236, 249))));
            sel.Setters.Add(new Setter(Control.ForegroundProperty,
                new SolidColorBrush(Color.FromRgb(0, 84, 153))));
            style.Triggers.Add(sel);

            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Control.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(237, 241, 246))));
            style.Triggers.Add(hover);

            return style;
        }

        // ── Список разделов с фильтрацией по роли ─────────────────────────

        private List<(string, UIElement)> BuildAllSections()
        {
            var list = new List<(string, UIElement)>();

            list.Add(("Начало работы", BuildStart()));

            if (Is("Admin", "Curator"))
                list.Add(("Студенты", BuildStudents()));

            if (Is("Admin", "Teacher", "Curator", "Headman", "Student"))
                list.Add(("Посещаемость", BuildAttendance()));

            if (Is("Admin", "Teacher", "Student"))
                list.Add(("Оценки", BuildGrades()));

            if (Is("Admin", "Teacher", "Headman", "Student"))
                list.Add(("Расписание", BuildSchedule()));

            if (Is("Admin", "Curator", "Student"))
                list.Add(("Мероприятия", BuildEvents()));

            if (Is("Admin", "Teacher", "Curator", "Headman", "Student"))
                list.Add(("Объявления", BuildAnnouncements()));

            if (Is("Admin", "Curator", "Student"))
                list.Add(("Соц. работа", BuildSocial()));

            if (Is("Admin"))
                list.Add(("Администрирование", BuildAdmin()));

            return list;
        }

        // ── Контент разделов ──────────────────────────────────────────────

        private UIElement BuildStart()
        {
            string roleDesc;
            switch (_role)
            {
                case "Admin":
                    roleDesc = "Вы работаете как Администратор.\n" +
                               "Вам доступны все разделы приложения.";
                    break;
                case "Teacher":
                    roleDesc = "Вы работаете как Преподаватель.\n" +
                               "Доступны: посещаемость ваших пар, расписание, оценки, объявления.";
                    break;
                case "Curator":
                    roleDesc = "Вы работаете как Куратор.\n" +
                               "Доступны: студенты и посещаемость вашей группы,\n" +
                               "мероприятия, объявления, социальная работа.";
                    break;
                case "Headman":
                    roleDesc = "Вы работаете как Старoста.\n" +
                               "Доступны: посещаемость своей группы, расписание, объявления.";
                    break;
                default:
                    roleDesc = "Вы работаете как Студент.\n" +
                               "Доступны: личная статистика посещаемости и оценок,\n" +
                               "расписание, мероприятия, объявления.";
                    break;
            }

            return Section(new[]
            {
                ("Навигация",
                 "Боковая панель слева содержит разделы приложения.\n" +
                 "Активный раздел подсвечивается. Кликните на пункт для перехода."),
                ("Ваша роль", roleDesc),
                ("Выход",
                 "Кнопка выхода в нижней части боковой панели.\n" +
                 "После выхода сессия очищается."),
                ("Справка",
                 "F1 в любом разделе — справка откроется на нужной теме."),
            });
        }

        private UIElement BuildStudents()
        {
            var items = new List<(string, string)>
            {
                ("Список",
                 "Все студенты с группой, куратором и контактными данными.\n" +
                 "Выводяться в таблице в соответствии с примененными фильтрами.")
            };

            if (Is("Admin"))
            {
                items.Add(("Добавление",
                    "Кнопка «Добавить» открывает форму создания.\n" +
                    "Заполните ФИО, группу и контактные данные."));
                items.Add(("Редактирование",
                    "Кнопка редактирования в строке.\n" +
                    "Можно изменить группу, статус, контакты."));
                items.Add(("Удаление",
                    "Мягкое удаление: запись скрывается,\n" +
                    "но остаётся в базе данных."));
            }

            return Section(items.ToArray());
        }

        private UIElement BuildAttendance()
        {
            var items = new List<(string, string)>
            {
                ("Карточки-фильтры",
                 "Кликните на карточку Присутствовал / Отсутствовал / Опоздал /\n" +
                 "Уважит. причина, чтобы отфильтровать таблицу.\n" +
                 "Повторный клик снимает фильтр."),
                ("Фильтры",
                 "Поиск — по имени студента и дисциплине.\n" +
                 "Даты — ограничение диапазона выборки.\n" +
                 "Дисциплина — конкретный предмет." +
                 (Is("Admin") ? "\nГруппа — фильтр по группе (только Admin)." : "") +
                 "\n«Сбросить» — сброс всех фильтров."),
                ("Пагинация",
                 "Кнопки страниц внизу таблицы.\n" +
                 "Справа выбирается количество строк на странице."),
            };

            if (_role == "Student")
            {
                items.Insert(0, ("Личная статистика",
                    "Отображаются ваши карточки по статусам и полосы\n" +
                    "посещаемости по каждой дисциплине, а также история посещений."));
            }

            if (Is("Admin", "Teacher", "Headman"))
            {
                items.Add(("Отметка посещаемости",
                    "Кнопка «Отметить» открывает окно отметки.\n" +
                    "Выберите дату и занятие, нажмите нужный статус для каждого студента.\n" +
                    "Уже выставленные отметки загружаются автоматически."));
            }

            if (Is("Admin"))
            {
                items.Add(("Редактирование / удаление",
                    "Кнопки в каждой строке: изменить статус или удалить запись."));
            }

            items.Add(("Экспорт",
                "Кнопка «Экспорт» сохраняет все отфильтрованные записи\n" +
                "(не только текущую страницу) в файл Excel."));

            return Section(items.ToArray());
        }

        private UIElement BuildGrades()
        {
            var items = new List<(string, string)>
            {
                ("Просмотр",
                 _role == "Student"
                     ? "Ваши оценки по дисциплинам с фильтрацией\n" +
                       "по предмету и типу работы."
                     : "Таблица с оценками по дисциплинам.\n" +
                       "Фильтрация по студенту, дисциплине и типу работы.")
            };

            if (Is("Admin", "Teacher"))
            {
                items.Add(("Выставление",
                    "Нажмите кнопку добавления или редактирования оценки.\n" +
                    "Укажите студента, дисциплину, тип работы и оценку."));
                items.Add(("Экспорт",
                    "Экспорт ведомости в файл Excel."));
            }

            return Section(items.ToArray());
        }

        private UIElement BuildSchedule()
        {
            var items = new List<(string, string)>
            {
                ("Просмотр",
                 _role == "Student" || _role == "Headman"
                     ? "Расписание вашей группы по неделям."
                     : _role == "Teacher"
                         ? "Ваше расписание пар по неделям."
                         : "Расписание по неделям."),
                ("Навигация",
                 "Стрелки для перехода между неделями,\n" +
                 "клик на конкретный день."),
            };

            if (Is("Admin"))
            {
                items.Add(("Управление",
                    "Добавление, изменение и удаление занятий.\n" +
                    "Укажите предмет, группу, аудиторию и время."));
            }

            return Section(items.ToArray());
        }

        private UIElement BuildEvents()
        {
            var items = new List<(string, string)>
            {
                ("Список",
                 "Предстоящие и прошедшие мероприятия\n" +
                 "с датой, местом и описанием."),
            };

            if (Is("Admin", "Curator"))
            {
                items.Add(("Добавление",
                    "Создайте мероприятие: название, дата, место, ответственный."));
            }

            items.Add(("Участники",
                "К каждому мероприятию прикрепляется список участников."));

            return Section(items.ToArray());
        }

        private UIElement BuildAnnouncements()
        {
            var canCreate = Is("Admin", "Teacher", "Curator");
            return Section(new[]
            {
                ("Объявления",
                 canCreate
                     ? "Публикация новостей и сообщений для студентов.\n" +
                       "Нажмите «Создать», заполните текст и нажмите «Опубликовать»."
                     : "Новости и сообщения от преподавателей и куратора."),
                ("Задания",
                 canCreate
                     ? "Создание учебных заданий: описание, срок, файлы."
                     : "Учебные задания: описание и срок сдачи."),
                ("Документы",
                 "Общие документы группы или кафедры."),
            });
        }

        private UIElement BuildSocial()
        {
            return Section(new[]
            {
                ("Социальная работа",
                 Is("Admin", "Curator")
                     ? "Учёт участия студентов в общественной жизни:\n" +
                       "волонтёрство, дежурства, комиссии."
                     : "Ваше участие в общественной жизни:\n" +
                       "волонтёрство, дежурства, комиссии."),
                ("Достижения",
                 Is("Admin", "Curator")
                     ? "Награды, дипломы и победы студентов\n" +
                       "в олимпиадах, соревнованиях и конкурсах."
                     : "Ваши награды, дипломы и победы\n" +
                       "в олимпиадах, соревнованиях и конкурсах."),
            });
        }

        private UIElement BuildAdmin() => Section(new[]
        {
            ("Пользователи",
             "Создание, редактирование и блокировка учётных записей.\n" +
             "Назначение ролей: Admin, Teacher, Curator, Headman, Student."),
            ("Группы",
             "Управление учебными группами, назначение куратора,\n" +
             "перевод студентов."),
            ("Предметы",
             "Справочник дисциплин с привязкой преподавателей."),
            ("Расписание",
             "Полное управление расписанием: занятия, аудитории, время."),
            ("Журнал аудита",
             "История действий пользователей: кто, когда и что изменил."),
            ("Мягкое удаление",
             "Записи помечаются IsDeleted = 1 и скрываются из интерфейса,\n" +
             "но остаются в базе данных."),
        });

        // ── Построитель панели раздела ─────────────────────────────────────

        private static UIElement Section((string label, string text)[] items)
        {
            var sp = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };

            foreach (var (label, text) in items)
            {
                var row = new Border
                {
                    Background      = Brushes.White,
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(225, 227, 230)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding         = new Thickness(0, 10, 0, 10)
                };

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lbl = new TextBlock
                {
                    Text              = label,
                    FontSize          = 12,
                    FontWeight        = FontWeights.SemiBold,
                    Foreground        = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    VerticalAlignment = VerticalAlignment.Top,
                    TextWrapping      = TextWrapping.Wrap,
                    Margin            = new Thickness(12, 0, 12, 0)
                };
                Grid.SetColumn(lbl, 0);
                g.Children.Add(lbl);

                var desc = new TextBlock
                {
                    Text         = text,
                    FontSize     = 12,
                    Foreground   = new SolidColorBrush(Color.FromRgb(80, 82, 86)),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight   = 19,
                    Margin       = new Thickness(0, 0, 12, 0)
                };
                Grid.SetColumn(desc, 1);
                g.Children.Add(desc);

                row.Child = g;
                sp.Children.Add(row);
            }

            return sp;
        }
    }
}
