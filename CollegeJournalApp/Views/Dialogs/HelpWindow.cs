using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

        private ListBox      _nav;
        private ScrollViewer _content;
        private List<(string Title, UIElement Panel)> _sections;

        public HelpWindow(string currentPageName = null, Window owner = null)
        {
            Title                 = "Справка";
            Width                 = 760;
            Height                = 580;
            MinWidth              = 560;
            MinHeight             = 380;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode            = ResizeMode.CanResizeWithGrip;
            FontFamily            = new FontFamily("Segoe UI");
            Background            = new SolidColorBrush(Color.FromRgb(245, 246, 247));
            Owner                 = owner;

            _sections = BuildAllSections();
            BuildWindow();

            int idx = 0;
            if (currentPageName != null)
                PageSectionMap.TryGetValue(currentPageName, out idx);
            _nav.SelectedIndex = idx;
        }

        private void BuildWindow()
        {
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Левая панель навигации
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

            // Правая панель контента
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

        // ── Разделы ────────────────────────────────────────────────────────

        private List<(string, UIElement)> BuildAllSections() =>
            new List<(string, UIElement)>
            {
                ("Начало работы",     BuildStart()),
                ("Студенты",          BuildStudents()),
                ("Посещаемость",      BuildAttendance()),
                ("Оценки",            BuildGrades()),
                ("Расписание",        BuildSchedule()),
                ("Мероприятия",       BuildEvents()),
                ("Объявления",        BuildAnnouncements()),
                ("Соц. работа",       BuildSocial()),
                ("Администрирование", BuildAdmin()),
            };

        private UIElement BuildStart() => Section(new[]
        {
            ("Навигация",
             "Боковая панель слева содержит разделы приложения.\n" +
             "Активный раздел подсвечивается. Кликните на любой пункт для перехода."),
            ("Роли",
             "Администратор — полный доступ.\n" +
             "Куратор — посещаемость и студенты своей группы.\n" +
             "Преподаватель — расписание и посещаемость своих пар.\n" +
             "Старoста — посещаемость своей группы.\n" +
             "Студент — личная статистика."),
            ("Выход",
             "Кнопка выхода в нижней части боковой панели.\n" +
             "После выхода сессия очищается и открывается окно входа."),
            ("Справка",
             "F1 в любом разделе — справка откроется на нужной теме."),
        });

        private UIElement BuildStudents() => Section(new[]
        {
            ("Список",
             "Все студенты с группой, куратором и контактными данными.\n" +
             "Поиск по ФИО, фильтр по группе."),
            ("Добавление",
             "Кнопка «Добавить» (Admin) открывает форму создания.\n" +
             "Заполните ФИО, группу и контактные данные."),
            ("Редактирование",
             "Кнопка редактирования в строке (Admin).\n" +
             "Можно изменить группу, статус, контакты."),
            ("Удаление",
             "Мягкое удаление (Admin): запись скрывается,\n" +
             "но остаётся в базе данных."),
        });

        private UIElement BuildAttendance() => Section(new[]
        {
            ("Карточки-фильтры",
             "Кликните на карточку Присутствовал / Отсутствовал / Опоздал /\n" +
             "Уважит. причина, чтобы отфильтровать таблицу.\n" +
             "Повторный клик снимает фильтр."),
            ("Фильтры",
             "Поиск — по имени студента и дисциплине.\n" +
             "Даты — ограничение диапазона выборки.\n" +
             "Дисциплина — конкретный предмет.\n" +
             "Группа — только для Администратора.\n" +
             "«Сбросить» — сброс всех фильтров."),
            ("Пагинация",
             "Кнопки страниц внизу таблицы.\n" +
             "Справа выбирается количество строк на странице."),
            ("Отметка посещаемости",
             "Кнопка «Отметить» (Преподаватель / Старoста / Admin).\n" +
             "Выберите дату и занятие, нажмите нужный статус для каждого студента.\n" +
             "Уже выставленные отметки загружаются автоматически."),
            ("Редактирование / удаление",
             "Кнопки в строке (Admin): изменить статус или удалить запись."),
            ("Экспорт",
             "Кнопка «Экспорт» сохраняет все отфильтрованные записи\n" +
             "(не только текущую страницу) в файл Excel."),
        });

        private UIElement BuildGrades() => Section(new[]
        {
            ("Просмотр",
             "Таблица с оценками по дисциплинам.\n" +
             "Фильтрация по студенту, дисциплине и типу работы."),
            ("Выставление",
             "Преподаватель выставляет оценки своим студентам.\n" +
             "Администратор может редактировать любую оценку."),
            ("Экспорт",
             "Экспорт ведомости в файл Excel."),
        });

        private UIElement BuildSchedule() => Section(new[]
        {
            ("Просмотр",
             "Расписание по неделям.\n" +
             "Каждый пользователь видит только своё расписание."),
            ("Навигация",
             "Стрелки для перехода между неделями,\n" +
             "клик на конкретный день."),
            ("Управление",
             "Администратор добавляет, изменяет и удаляет занятия.\n" +
             "Укажите предмет, группу, аудиторию и время."),
        });

        private UIElement BuildEvents() => Section(new[]
        {
            ("Список",
             "Предстоящие и прошедшие мероприятия\n" +
             "с датой, местом и описанием."),
            ("Добавление",
             "Куратор и Администратор создают мероприятия:\n" +
             "название, дата, место, ответственный."),
            ("Участники",
             "К каждому мероприятию прикрепляется список участников."),
        });

        private UIElement BuildAnnouncements() => Section(new[]
        {
            ("Объявления",
             "Новости и сообщения для студентов.\n" +
             "Преподаватели и Администратор могут создавать объявления."),
            ("Задания",
             "Учебные задания: описание, срок, прикреплённые файлы."),
            ("Документы",
             "Общие документы группы или кафедры."),
        });

        private UIElement BuildSocial() => Section(new[]
        {
            ("Социальная работа",
             "Учёт участия студентов в общественной жизни:\n" +
             "волонтёрство, дежурства, комиссии."),
            ("Достижения",
             "Награды, дипломы и победы в олимпиадах,\n" +
             "соревнованиях и конкурсах."),
        });

        private UIElement BuildAdmin() => Section(new[]
        {
            ("Пользователи",
             "Создание, редактирование и блокировка учётных записей.\n" +
             "Назначение ролей."),
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

        // ── Построитель контентной панели ──────────────────────────────────

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
                    Padding         = new Thickness(0, 10, 0, 10),
                    Margin          = new Thickness(0)
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
