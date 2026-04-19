# EduTrack Pro — Электронный журнал колледжа

> Десктопное WPF-приложение для ведения электронного журнала колледжа.  
> Платформа: **.NET Framework 4.8** · БД: **MS SQL Server** · Паттерн: **Code-Behind**

---

## Содержание

1. [Обзор проекта](#1-обзор-проекта)
2. [Технический стек](#2-технический-стек)
3. [Структура проекта](#3-структура-проекта)
4. [Роли и права доступа](#4-роли-и-права-доступа)
5. [Функциональные модули](#5-функциональные-модули)
6. [Словарь данных (таблицы БД)](#6-словарь-данных-таблицы-бд)
7. [Хранимые процедуры](#7-хранимые-процедуры)
8. [Вспомогательные классы](#8-вспомогательные-классы)
9. [Установка и запуск](#9-установка-и-запуск)
10. [Тестовые учётные записи](#10-тестовые-учётные-записи)
11. [Архитектура и поток данных](#11-архитектура-и-поток-данных)
12. [Безопасность](#12-безопасность)

---

## 1. Обзор проекта

**EduTrack Pro** — многоролевая система управления учебным процессом колледжа.  
Приложение охватывает весь жизненный цикл работы со студентами: от зачисления до выдачи документов.

### Ключевые возможности

- Ролевая модель (5 ролей) с разграничением видимости и редактирования
- Электронный журнал оценок с темами уроков и экспортом в Excel
- Учёт посещаемости с отметкой в реальном времени
- Расписание с импортом из Excel
- Объявления с вложениями
- Карточки студентов (фото, родители, социальный статус, достижения, документы)
- Дашборд с KPI-метриками для каждой роли
- Журнал аудита всех действий пользователей

---

## 2. Технический стек

| Компонент | Технология |
|-----------|-----------|
| Фреймворк | .NET Framework 4.8 |
| UI | WPF (Windows Presentation Foundation) |
| СУБД | Microsoft SQL Server 2019+ |
| Доступ к данным | Microsoft.Data.SqlClient (ADO.NET) |
| Паттерн UI | Code-Behind (без MVVM) |
| Привязки | IValueConverter + DataTable-пивот |
| Excel | ClosedXML |

### NuGet-пакеты

| Пакет | Версия | Назначение |
|-------|--------|-----------|
| `Microsoft.Data.SqlClient` | 7.0.0 | Подключение к SQL Server |
| `ClosedXML` | последняя | Чтение/запись Excel (.xlsx) |
| `DocumentFormat.OpenXml` | 3.1.1 | Зависимость ClosedXML |
| `Microsoft.IdentityModel.Tokens` | 8.16.0 | JWT-инфраструктура (зависимость) |

---

## 3. Структура проекта

```
CollegeJournalApp/
├── CollegeJournalApp/
│   ├── App.xaml(.cs)                  # Точка входа, глобальные ресурсы
│   ├── MainWindow.xaml(.cs)           # Главное окно с навигацией
│   │
│   ├── Database/
│   │   ├── DatabaseHelper.cs          # ADO.NET-обёртка (ExecuteProcedure, ExecuteNonQuery)
│   │   └── Scripts/                   # SQL-скрипты (таблицы, хранимые процедуры, сиды)
│   │       ├── seed_data.sql
│   │       ├── fresh_seed.sql
│   │       ├── nuke_and_seed.sql
│   │       ├── cleanup_seed.sql
│   │       ├── migration_*.sql
│   │       └── sp_*.sql               # ~60 хранимых процедур
│   │
│   ├── Helpers/
│   │   └── SessionHelper.cs           # Текущий пользователь (синглтон-состояние сессии)
│   │
│   └── Views/
│       ├── LoginWindow.xaml(.cs)      # Форма входа
│       ├── StudentCardWindow.xaml(.cs)# Карточка студента (модальное окно)
│       ├── AuditDetailWindow.xaml(.cs)# Детали записи аудит-лога
│       │
│       ├── Pages/
│       │   ├── DashboardPage.xaml(.cs)    # Дашборд (KPI, события, алерты)
│       │   ├── GradesPage.xaml(.cs)       # Журнал оценок + темы уроков
│       │   ├── AttendancePage.xaml(.cs)   # Посещаемость
│       │   ├── SchedulePage.xaml(.cs)     # Расписание + импорт Excel
│       │   ├── StudentsPage.xaml(.cs)     # Список студентов
│       │   ├── AnnouncementsPage.xaml(.cs)# Объявления
│       │   ├── AdminPage.xaml(.cs)        # Панель администратора
│       │   ├── AuditPage.xaml(.cs)        # Журнал аудита
│       │   └── StubPages.cs               # Заглушки страниц
│       │
│       └── Dialogs/
│           ├── StudentEditDialog.xaml(.cs)
│           ├── UserEditDialog.xaml(.cs)
│           ├── TeacherEditDialog.xaml(.cs)
│           ├── GroupEditDialog.xaml(.cs)
│           ├── ScheduleEditDialog.xaml(.cs)
│           ├── SubjectEditDialog.xaml(.cs)
│           ├── AcademicYearEditDialog.xaml(.cs)
│           ├── DormitoryEditDialog.xaml(.cs)
│           ├── CuratorAssignDialog.xaml(.cs)
│           ├── MarkAttendanceWindow.xaml(.cs)
│           └── HelpWindow.cs
│
└── packages/                          # NuGet-пакеты (восстанавливаются автоматически)
```

---

## 4. Роли и права доступа

| Функция | Admin | Teacher | Curator | Headman | Student |
|---------|:-----:|:-------:|:-------:|:-------:|:-------:|
| Дашборд (KPI системы) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Дашборд (своя группа) | ❌ | ❌ | ✅ | ✅ | ✅ |
| Журнал оценок — просмотр | ✅ | ✅ | ✅ | ✅ | ✅* |
| Журнал оценок — редактирование | ❌ | ✅** | ❌ | ❌ | ❌ |
| Темы уроков — просмотр | ✅ | ✅ | ✅ | ✅ | ✅ |
| Темы уроков — редактирование | ❌ | ✅** | ❌ | ❌ | ❌ |
| Посещаемость — просмотр | ✅ | ✅ | ✅ | ✅ | ✅* |
| Посещаемость — отметка | ❌ | ✅ | ❌ | ✅*** | ❌ |
| Расписание — просмотр | ✅ | ✅ | ✅ | ✅ | ✅ |
| Расписание — редактирование | ✅ | ❌ | ❌ | ❌ | ❌ |
| Импорт расписания из Excel | ✅ | ❌ | ❌ | ❌ | ❌ |
| Студенты — просмотр | ✅ | ✅ | ✅ | ✅**** | ❌ |
| Студенты — добавление/редактирование | ✅ | ❌ | ❌ | ❌ | ❌ |
| Объявления — просмотр | ✅ | ✅ | ✅ | ✅ | ✅ |
| Объявления — создание | ✅ | ✅ | ✅ | ❌ | ❌ |
| Карточка студента — полная | ✅ | ✅ | ✅ | ❌ | ❌ |
| Управление пользователями | ✅ | ❌ | ❌ | ❌ | ❌ |
| Управление преподавателями | ✅ | ❌ | ❌ | ❌ | ❌ |
| Управление группами | ✅ | ❌ | ❌ | ❌ | ❌ |
| Журнал аудита | ✅ | ❌ | ❌ | ❌ | ❌ |
| Справочники (предметы, года, общежития) | ✅ | ❌ | ❌ | ❌ | ❌ |

\* Только свои данные  
\** Только по своим предметам/группам  
\*** Ограниченная отметка (только своя группа)  
\**** Только своя группа

---

## 5. Функциональные модули

### 5.1 Дашборд

**Администратор** видит:
- KPI-карточки: всего пользователей, студентов, групп, преподавателей, средний балл, входов сегодня, действий сегодня, новых пользователей в этом месяце
- Таблицу статистики по группам (студентов, ср. балл, двоечники, отличники %)
- Посещаемость сегодня по каждой группе (прогресс-бар: присутствует/отсутствует/опоздал)
- Ленту последних событий аудит-лога
- Алерты: студенты с ≥2 двойками за месяц или ≥4 пропусками за 30 дней

**Куратор** видит:
- Свою группу, количество студентов, средний балл, посещаемость (%)
- Пропуски за последние 30 дней
- Ближайшие события группы

**Студент / Староста** видит:
- Свою группу, личный средний балл, уроков сегодня, пропусков за 30 дней
- Процент посещаемости за 30 дней
- Расписание на сегодня

### 5.2 Электронный журнал оценок

- Выбор группы, предмета, месяца/года
- Отображение в виде сетки «студент × день месяца»
- Оценки 2–5, выделение цветом (5=зелёный, 4=синий, 3=жёлтый, 2=красный)
- Специальная строка «📝 Тема урока» — первая строка таблицы (жёлтый фон)
- **Темы уроков**: преподаватель кликает по ячейке темы → всплывающий попап с TextBox → сохраняет через `sp_SaveLessonNote`
- **Просмотр тем**: ячейка показывает «•», при наведении — полный текст темы (Tooltip); заголовок дня выделяется синим с подчёркиванием, при наведении — текст темы
- Экспорт журнала в Excel (ClosedXML)
- Итоговый/месячный журнал (переключение режимов)

### 5.3 Посещаемость

- Выбор группы, даты, урока
- Для каждого студента: Присутствовал / Отсутствовал / Опоздал / Болен
- Отчёт по посещаемости: фильтр по группе и периоду, экспорт в Excel
- Отметка через окно `MarkAttendanceWindow`

### 5.4 Расписание

- Просмотр недельного расписания по группе
- Режимы: чётная/нечётная/обе недели
- Редактирование (добавить/изменить/удалить занятие) — только Admin
- **Импорт из Excel**: формат 7 столбцов (день, №, время, дисциплина, аудитория, преподаватель, тип недели)
- Просмотр расписания преподавателя

### 5.5 Студенты

- Полный список с фильтром по группе, поиском по ФИО
- Карточка студента: личные данные, фото (загрузка/удаление), контакты, социальный статус, родители, достижения, документы, оценки, посещаемость
- Добавление/редактирование студентов (Admin)
- Soft-delete (мягкое удаление)

### 5.6 Объявления

- Публикация объявлений с вложениями (файлы)
- Закрепление объявлений (pin)
- Видимость: все роли читают; Admin/Teacher/Curator могут публиковать

### 5.7 Панель администратора

Вкладки:
- **Пользователи** — CRUD, смена пароля (SHA2_256)
- **Студенты** — добавление с автогенерацией студенческого билета
- **Группы** — создание/редактирование, назначение куратора
- **Преподаватели** — управление штатом
- **Предметы** — справочник дисциплин
- **Учебные годы** — академические периоды
- **Общежития** — база общежитий

### 5.8 Журнал аудита

- Полный лог всех действий: LOGIN, CREATE, UPDATE, DELETE, EXPORT и др.
- Фильтр по дате, пользователю, типу действия
- Детальный просмотр в `AuditDetailWindow` (двойной клик по строке)

---

## 6. Словарь данных (таблицы БД)

База данных: **`CollegeJournal`**

---

### `dbo.Roles`

Справочник ролей пользователей.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `RoleId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `RoleName` | NVARCHAR(50) | NOT NULL UNIQUE | Системное имя роли |
| `DisplayName` | NVARCHAR(100) | NOT NULL | Отображаемое название |

**Значения:** `Admin`, `Teacher`, `Curator`, `Headman`, `Student`

---

### `dbo.Users`

Учётные записи системы.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `UserId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `Login` | NVARCHAR(100) | NOT NULL UNIQUE | Логин для входа |
| `PasswordHash` | NVARCHAR(200) | NOT NULL | Хеш пароля (SHA2_256 / BCrypt) |
| `RoleId` | INT | NOT NULL FK → Roles | Роль пользователя |
| `FirstName` | NVARCHAR(100) | NOT NULL | Имя |
| `LastName` | NVARCHAR(100) | NOT NULL | Фамилия |
| `MiddleName` | NVARCHAR(100) | NULL | Отчество |
| `Email` | NVARCHAR(200) | NULL | Электронная почта |
| `Phone` | NVARCHAR(30) | NULL | Телефон |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |
| `CreatedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата создания |
| `UpdatedAt` | DATETIME | NULL | Дата последнего изменения |

---

### `dbo.Groups`

Учебные группы.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `GroupId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `GroupName` | NVARCHAR(50) | NOT NULL UNIQUE | Название группы |
| `Course` | TINYINT | NOT NULL | Курс (1–4) |
| `CuratorId` | INT | NULL FK → Users | Куратор группы |
| `IsGraduated` | BIT | NOT NULL DEFAULT 0 | Выпускная группа |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |
| `CreatedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата создания |

---

### `dbo.Students`

Студенты (расширение Users).

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `StudentId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `UserId` | INT | NOT NULL FK → Users | Ссылка на учётную запись |
| `GroupId` | INT | NOT NULL FK → Groups | Группа студента |
| `StudentCode` | NVARCHAR(20) | NOT NULL UNIQUE | Номер студенческого билета |
| `BirthDate` | DATE | NULL | Дата рождения |
| `Address` | NVARCHAR(500) | NULL | Адрес проживания |
| `EnrollmentDate` | DATE | NULL | Дата зачисления |
| `PhotoPath` | NVARCHAR(500) | NULL | Путь к фото на диске |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |
| `CreatedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата создания |

---

### `dbo.Teachers`

Преподаватели (расширение Users).

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `TeacherId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `UserId` | INT | NULL FK → Users | Ссылка на учётную запись |
| `LastName` | NVARCHAR(100) | NOT NULL | Фамилия |
| `FirstName` | NVARCHAR(100) | NOT NULL | Имя |
| `MiddleName` | NVARCHAR(100) | NULL | Отчество |
| `Specialization` | NVARCHAR(200) | NULL | Специализация |
| `Phone` | NVARCHAR(30) | NULL | Телефон |
| `Email` | NVARCHAR(200) | NULL | Email |
| `IsActive` | BIT | NOT NULL DEFAULT 1 | Работает в учреждении |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |
| `CreatedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата создания |

---

### `dbo.Subjects`

Справочник учебных дисциплин.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `SubjectId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `SubjectName` | NVARCHAR(200) | NOT NULL UNIQUE | Название дисциплины |
| `ShortName` | NVARCHAR(20) | NULL | Аббревиатура |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |

---

### `dbo.Schedule`

Расписание занятий.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `ScheduleId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `GroupId` | INT | NOT NULL FK → Groups | Группа |
| `SubjectId` | INT | NOT NULL FK → Subjects | Предмет |
| `TeacherId` | INT | NOT NULL FK → Teachers | Преподаватель |
| `DayOfWeek` | TINYINT | NOT NULL | День недели (1=Пн … 6=Сб) |
| `LessonNumber` | TINYINT | NOT NULL | Номер урока |
| `StartTime` | TIME | NOT NULL | Время начала |
| `EndTime` | TIME | NOT NULL | Время окончания |
| `Classroom` | NVARCHAR(30) | NULL | Кабинет |
| `WeekType` | NVARCHAR(10) | NOT NULL DEFAULT 'Обе' | Чётная / Нечётная / Обе |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |

---

### `dbo.Grades`

Оценки студентов по дням.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `GradeId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `StudentId` | INT | NOT NULL FK → Students | Студент |
| `SubjectId` | INT | NOT NULL FK → Subjects | Предмет |
| `GradeValue` | TINYINT | NOT NULL | Оценка (2–5) |
| `GradeDate` | DATE | NOT NULL | Дата выставления |
| `TeacherId` | INT | NULL FK → Teachers | Кто выставил |
| `Comment` | NVARCHAR(200) | NULL | Комментарий преподавателя |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |
| `CreatedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата создания |

**Уникальный ключ:** (StudentId, SubjectId, GradeDate) — одна оценка в день по предмету.

---

### `dbo.MonthlyGrades`

Итоговые оценки за месяц/период.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `MonthlyGradeId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `StudentId` | INT | NOT NULL FK → Students | Студент |
| `SubjectId` | INT | NOT NULL FK → Subjects | Предмет |
| `GradeValue` | TINYINT | NOT NULL | Итоговая оценка |
| `GradeYear` | SMALLINT | NOT NULL | Учебный год |
| `GradeMonth` | TINYINT | NOT NULL | Месяц (1–12) |
| `TeacherId` | INT | NULL FK → Teachers | Кто выставил |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |

---

### `dbo.LessonNotes`

Темы / заметки к урокам (создана в текущей версии).

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `NoteId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `GroupId` | INT | NOT NULL FK → Groups | Группа |
| `SubjectId` | INT | NOT NULL FK → Subjects | Предмет |
| `LessonDate` | DATE | NOT NULL | Дата урока |
| `NoteText` | NVARCHAR(300) | NULL | Текст темы / заметки |
| `CreatedByUserId` | INT | NULL FK → Users | Кто создал |
| `CreatedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата создания |
| `UpdatedAt` | DATETIME | NULL | Дата обновления |

**Уникальный ключ:** (GroupId, SubjectId, LessonDate) — одна заметка на урок.

---

### `dbo.Attendance`

Посещаемость.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `AttendanceId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `StudentId` | INT | NOT NULL FK → Students | Студент |
| `LessonDate` | DATE | NOT NULL | Дата занятия |
| `LessonNumber` | TINYINT | NULL | Номер урока |
| `Status` | NVARCHAR(30) | NOT NULL | Присутствовал / Отсутствовал / Опоздал / Болен |
| `MarkedByUserId` | INT | NULL FK → Users | Кто отметил |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |
| `CreatedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата создания |

---

### `dbo.Parents`

Родители / законные представители студентов.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `ParentId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `StudentId` | INT | NOT NULL FK → Students | Студент |
| `FullName` | NVARCHAR(200) | NOT NULL | ФИО родителя |
| `Relation` | NVARCHAR(50) | NULL | Отец / Мать / Опекун |
| `Phone` | NVARCHAR(30) | NULL | Телефон |
| `Email` | NVARCHAR(200) | NULL | Email |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |

---

### `dbo.StudentSocialInfo`

Социальный статус студента.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `SocialInfoId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `StudentId` | INT | NOT NULL FK → Students UNIQUE | Студент (1:1) |
| `IsOrphan` | BIT | NOT NULL DEFAULT 0 | Сирота |
| `IsDisabled` | BIT | NOT NULL DEFAULT 0 | Инвалидность |
| `IsLowIncome` | BIT | NOT NULL DEFAULT 0 | Малообеспеченная семья |
| `DormitoryId` | INT | NULL FK → Dormitories | Общежитие (если проживает) |
| `RoomNumber` | NVARCHAR(10) | NULL | Номер комнаты |
| `Notes` | NVARCHAR(500) | NULL | Дополнительные сведения |
| `UpdatedAt` | DATETIME | NULL | Дата обновления |

---

### `dbo.Achievements`

Достижения студентов (олимпиады, конкурсы и т.п.).

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `AchievementId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `StudentId` | INT | NOT NULL FK → Students | Студент |
| `Title` | NVARCHAR(300) | NOT NULL | Название достижения |
| `AchievementDate` | DATE | NULL | Дата |
| `Level` | NVARCHAR(50) | NULL | Уровень (региональный, всероссийский…) |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |
| `CreatedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата создания |

---

### `dbo.Announcements`

Объявления.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `AnnouncementId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `Title` | NVARCHAR(300) | NOT NULL | Заголовок |
| `Body` | NVARCHAR(MAX) | NULL | Текст объявления |
| `AuthorUserId` | INT | NOT NULL FK → Users | Автор |
| `PublishedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Дата публикации |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |

---

### `dbo.AnnouncementAttachments`

Вложения к объявлениям.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `AttachmentId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `AnnouncementId` | INT | NOT NULL FK → Announcements | Объявление |
| `FileName` | NVARCHAR(300) | NOT NULL | Имя файла |
| `FilePath` | NVARCHAR(500) | NOT NULL | Путь на диске / URL |
| `FileSize` | BIGINT | NULL | Размер в байтах |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |

---

### `dbo.AnnouncementPins`

Закреплённые объявления (многие-ко-многим: объявление × роль или пользователь).

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `PinId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `AnnouncementId` | INT | NOT NULL FK → Announcements | Объявление |
| `PinnedByUserId` | INT | NULL FK → Users | Кто закрепил |
| `PinnedAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Когда закреплено |

---

### `dbo.Documents`

Документы студентов.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `DocumentId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `StudentId` | INT | NOT NULL FK → Students | Студент |
| `DocType` | NVARCHAR(100) | NOT NULL | Тип документа |
| `DocNumber` | NVARCHAR(100) | NULL | Номер документа |
| `IssuedBy` | NVARCHAR(300) | NULL | Кем выдан |
| `IssuedDate` | DATE | NULL | Дата выдачи |
| `FilePath` | NVARCHAR(500) | NULL | Скан документа |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |

---

### `dbo.AcademicYears`

Справочник учебных годов.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `AcademicYearId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `YearLabel` | NVARCHAR(20) | NOT NULL UNIQUE | Метка (напр. «2024–2025») |
| `StartDate` | DATE | NOT NULL | Начало учебного года |
| `EndDate` | DATE | NOT NULL | Конец учебного года |
| `IsCurrent` | BIT | NOT NULL DEFAULT 0 | Текущий год |

---

### `dbo.Dormitories`

Справочник общежитий.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `DormitoryId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `DormName` | NVARCHAR(100) | NOT NULL | Название |
| `Address` | NVARCHAR(300) | NULL | Адрес |
| `TotalRooms` | INT | NULL | Общее число комнат |
| `IsDeleted` | BIT | NOT NULL DEFAULT 0 | Мягкое удаление |

---

### `dbo.AuditLog`

Журнал действий пользователей.

| Столбец | Тип | Null | Описание |
|---------|-----|------|----------|
| `AuditId` | INT IDENTITY | NOT NULL PK | Суррогатный ключ |
| `UserId` | INT | NULL FK → Users | Кто выполнил |
| `Action` | NVARCHAR(50) | NOT NULL | Тип действия (LOGIN, CREATE, UPDATE, DELETE, EXPORT, …) |
| `TableName` | NVARCHAR(100) | NULL | Затронутая таблица |
| `RecordId` | INT | NULL | ID затронутой записи |
| `Description` | NVARCHAR(500) | NULL | Описание действия |
| `ActionAt` | DATETIME | NOT NULL DEFAULT GETDATE() | Время действия |
| `IpAddress` | NVARCHAR(50) | NULL | IP-адрес (при наличии) |

---

## 7. Хранимые процедуры

Все хранимые процедуры находятся в схеме `dbo` базы `CollegeJournal`.  
Файлы расположены в `CollegeJournalApp/Database/Scripts/`.

### 7.1 Аутентификация и пользователи

| Процедура | Файл | Описание |
|-----------|------|----------|
| `sp_AddUser` | sp_AddUser.sql | Создание пользователя (хеш через `HASHBYTES('SHA2_256',...)`) |
| `sp_UpdateUser` | sp_UpdateUser.sql | Обновление данных / смена пароля |

### 7.2 Дашборд

| Процедура | Файл | Параметры | Описание |
|-----------|------|-----------|----------|
| `sp_GetDashboard` | sp_GetDashboard.sql | `@UserId INT`, `@RoleName NVARCHAR(50)` | Возвращает одну строку KPI-метрик в зависимости от роли: Admin / Curator / Headman / Student |
| `sp_GetDashboardEvents` | sp_GetDashboardEvents.sql | `@UserId INT`, `@RoleName NVARCHAR(50)` | Лента последних событий для дашборда |
| `sp_GetTodaySchedule` | sp_GetTodaySchedule.sql | `@UserId INT`, `@RoleName NVARCHAR(50)` | Расписание на сегодня |
| `sp_GetAdminGroupStats` | sp_GetAdminGroupStats.sql | — | Статистика по группам (Admin): ср. балл, двоечники, отличники |
| `sp_GetAdminAttendanceToday` | sp_GetAdminAttendanceToday.sql | — | Посещаемость всех групп сегодня |
| `sp_GetAdminAlerts` | sp_GetAdminAlerts.sql | — | TOP-20 проблемных студентов (≥2 двоек / ≥4 пропусков) |

### 7.3 Журнал оценок

| Процедура | Файл | Параметры | Описание |
|-----------|------|-----------|----------|
| `sp_GetGroupSubjects` | sp_GetGroupSubjects.sql | `@GroupId INT`, `@TeacherUserId INT` | Предметы группы (с фильтром по преподавателю) |
| `sp_GetStudentGrades` | sp_GetStudentGrades.sql | `@StudentId INT` | Оценки конкретного студента |
| `sp_GetLessonNotes` | sp_GetLessonNotes.sql | `@GroupId INT`, `@SubjectId INT`, `@Year INT`, `@Month INT` | Темы уроков за месяц (DayNum + NoteText) |
| `sp_SaveLessonNote` | sp_SaveLessonNote.sql | `@GroupId INT`, `@SubjectId INT`, `@LessonDate DATE`, `@NoteText NVARCHAR(300)`, `@UserId INT` | UPSERT темы урока; пустой текст → DELETE |

### 7.4 Посещаемость

| Процедура | Файл | Параметры | Описание |
|-----------|------|-----------|----------|
| `sp_GetAttendanceReport` | sp_GetAttendanceReport.sql | `@GroupId INT`, `@DateFrom DATE`, `@DateTo DATE` | Отчёт по посещаемости за период |
| `sp_GetLessonsForMarking` | sp_GetLessonsForMarking.sql | `@GroupId INT`, `@Date DATE` | Список уроков для отметки посещаемости |
| `sp_GetStudentsForMarking` | sp_GetStudentsForMarking.sql | `@GroupId INT` | Список студентов группы для отметки |
| `sp_SaveAttendanceMark` | sp_SaveAttendanceMark.sql | `@StudentId INT`, `@LessonDate DATE`, `@LessonNumber TINYINT`, `@Status NVARCHAR(30)`, `@MarkedByUserId INT` | UPSERT записи посещаемости |
| `sp_DeleteAttendanceMark` | sp_DeleteAttendanceMark.sql | `@AttendanceId INT` | Удаление отметки |
| `sp_GetStudentAttendance` | sp_GetStudentAttendance.sql | `@StudentId INT` | История посещаемости студента |

### 7.5 Расписание

| Процедура | Файл | Параметры | Описание |
|-----------|------|-----------|----------|
| `sp_GetScheduleAdmin` | sp_GetScheduleAdmin.sql | `@GroupId INT` | Расписание группы (Admin/редактирование) |
| `sp_GetCuratorSchedule` | sp_GetCuratorSchedule.sql | `@UserId INT` | Расписание для куратора |
| `sp_GetTeacherSchedule` | sp_GetTeacherSchedule.sql | `@TeacherId INT` | Расписание преподавателя |
| `sp_SaveScheduleItem` | sp_SaveScheduleItem.sql | ScheduleId, GroupId, SubjectId, TeacherId, DayOfWeek, LessonNumber, StartTime, EndTime, Classroom, WeekType | UPSERT записи расписания |
| `sp_DeleteScheduleItem` | sp_DeleteScheduleItem.sql | `@ScheduleId INT` | Soft-delete записи расписания |
| `sp_ImportScheduleItem` | sp_ImportScheduleItem.sql | Те же поля | Импорт строки из Excel (MERGE) |

### 7.6 Студенты

| Процедура | Файл | Параметры | Описание |
|-----------|------|-----------|----------|
| `sp_AddStudent` | sp_AddStudent.sql | Поля студента + UserId | Создание студента с автогенерацией кода |
| `sp_GetNextStudentCode` | sp_GetNextStudentCode.sql | — | Генерация следующего номера студенческого |
| `sp_GetStudentDetails` | sp_GetStudentDetails.sql | `@StudentId INT` | Полная карточка студента |
| `sp_GetStudentParents` | sp_GetStudentParents.sql | `@StudentId INT` | Родители студента |
| `sp_GetStudentSocial` | sp_GetStudentSocial.sql | `@StudentId INT` | Социальный статус |
| `sp_GetStudentAchievements` | sp_GetStudentAchievements.sql | `@StudentId INT` | Достижения студента |
| `sp_GetStudentDocuments` | sp_GetStudentDocuments.sql | `@StudentId INT` | Документы студента |
| `sp_UploadStudentPhoto` | sp_UploadStudentPhoto.sql | `@StudentId INT`, `@PhotoPath NVARCHAR(500)` | Сохранение пути к фото |
| `sp_DeleteStudentPhoto` | sp_DeleteStudentPhoto.sql | `@StudentId INT` | Очистка пути к фото |

### 7.7 Преподаватели

| Процедура | Файл | Описание |
|-----------|------|----------|
| `sp_GetTeachers` | sp_GetTeachers.sql | Список активных преподавателей (для выбора) |
| `sp_GetTeachersAll` | sp_GetTeachersAll.sql | Все преподаватели (Admin-панель) |
| `sp_GetAllTeachers` | sp_GetAllTeachers.sql | Полный список с деталями |
| `sp_GetTeachersForCurator` | sp_GetTeachersForCurator.sql | Преподаватели по группе куратора |
| `sp_AddTeacher` | sp_AddTeacher.sql | Добавление преподавателя |
| `sp_UpdateTeacher` | sp_UpdateTeacher.sql | Редактирование преподавателя |

### 7.8 Группы

| Процедура | Файл | Описание |
|-----------|------|----------|
| `sp_GetAllGroups` | sp_GetAllGroups.sql | Все группы (Admin-панель) |
| `sp_AssignCurator` | sp_AssignCurator.sql | Назначение куратора группы |

### 7.9 Справочники

| Процедура | Файл | Описание |
|-----------|------|----------|
| `sp_GetSubjectsAdmin` | sp_GetSubjectsAdmin.sql | Все предметы (Admin) |
| `sp_GetSubjectsAll` | sp_GetSubjectsAll.sql | Предметы для выпадающих списков |
| `sp_AddSubject` | sp_AddSubject.sql | Добавление предмета |
| `sp_UpdateSubject` | sp_UpdateSubject.sql | Редактирование предмета |
| `sp_GetAcademicYears` | sp_GetAcademicYears.sql | Учебные годы |
| `sp_AddAcademicYear` | sp_AddAcademicYear.sql | Добавление учебного года |
| `sp_UpdateAcademicYear` | sp_UpdateAcademicYear.sql | Редактирование учебного года |
| `sp_GetDormitories` | sp_GetDormitories.sql | Список общежитий |
| `sp_AddDormitory` | sp_AddDormitory.sql | Добавление общежития |
| `sp_UpdateDormitory` | sp_UpdateDormitory.sql | Редактирование общежития |

---

## 8. Вспомогательные классы

### `DatabaseHelper` (`Database/DatabaseHelper.cs`)

Статический класс-обёртка над ADO.NET.

| Метод | Подпись | Описание |
|-------|---------|----------|
| `GetConnection()` | `SqlConnection GetConnection()` | Открывает и возвращает соединение из `App.config` |
| `TestConnection()` | `bool TestConnection()` | Проверяет доступность сервера |
| `ExecuteProcedure()` | `DataTable ExecuteProcedure(string proc, SqlParameter[] p = null)` | Выполняет SP, возвращает DataTable; показывает MessageBox при ошибке |
| `ExecuteNonQuery()` | `int ExecuteNonQuery(string proc, SqlParameter[] p = null)` | Выполняет SP без возврата строк (INSERT/UPDATE/DELETE) |

Строка подключения берётся из `App.config` → `connectionStrings` → ключ `"CollegeJournal"`.

### `SessionHelper` (`Helpers/SessionHelper.cs`)

Статический класс — хранилище сессии текущего пользователя.

| Свойство / метод | Тип | Описание |
|-----------------|-----|----------|
| `UserId` | `int` | ID текущего пользователя |
| `Login` | `string` | Логин |
| `FullName` | `string` | ФИО (Фамилия Имя) |
| `RoleName` | `string` | Системное имя роли |
| `LastName` | `string` | Фамилия |
| `FirstName` | `string` | Имя |
| `IsAdmin` | `bool` | `RoleName == "Admin"` |
| `IsCurator` | `bool` | `RoleName == "Curator"` |
| `IsHeadman` | `bool` | `RoleName == "Headman"` |
| `IsStudent` | `bool` | `RoleName == "Student"` |
| `IsTeacher` | `bool` | `RoleName == "Teacher"` |
| `Clear()` | `void` | Сбрасывает все поля (выход из системы) |

### IValueConverter-конвертеры (в `GradesPage.xaml.cs`)

| Класс | Назначение |
|-------|-----------|
| `GradeTextConv` | Число → строка оценки; нечисловой текст → «•» (маркер темы урока) |
| `GradeBgConv` | Номер оценки → цвет фона ячейки (5=зелёный, 4=синий, 3=жёлтый, 2=красный) |
| `GradeForeConv` | Оценка → цвет текста; «•» → синий |
| `NoteTooltipConv` | Значение ячейки → текст тултипа (только для тем уроков) |

---

## 9. Установка и запуск

### Предварительные требования

- Windows 10/11
- Visual Studio 2019 или новее
- .NET Framework 4.8 (установлен в Windows 10/11 по умолчанию)
- Microsoft SQL Server 2019+ (или SQL Server Express)
- SQL Server Management Studio (SSMS)

### Шаги установки

**1. Клонирование репозитория**
```bash
git clone https://github.com/<ваш-репозиторий>/CollegeJournalApp.git
cd CollegeJournalApp
```

**2. Создание базы данных**

Откройте SSMS и выполните скрипты в следующем порядке:

```sql
-- Шаг 1: Создать БД и таблицы
-- Выполните скрипты из папки Database/Scripts/ в порядке:
-- 1. Создание таблиц (если есть create_tables.sql или schema.sql)
-- 2. fresh_seed.sql  — начальные справочники и тестовые данные

-- Шаг 2: Хранимые процедуры (все sp_*.sql файлы)
-- Удобно выполнить через: Database/Scripts/nuke_and_seed.sql
```

**3. Настройка строки подключения**

Откройте `CollegeJournalApp/App.config`, найдите секцию `connectionStrings`:

```xml
<connectionStrings>
  <add name="CollegeJournal"
       connectionString="Server=localhost;Database=CollegeJournal;Trusted_Connection=True;TrustServerCertificate=True;"
       providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```

Замените `localhost` на имя вашего SQL Server-экземпляра.

**4. Применение миграций (для существующих БД)**

```sql
-- Таблица тем уроков (если обновляете с предыдущей версии):
-- Database/Scripts/migration_lesson_notes.sql

-- Хранимые процедуры дашборда (обновлённые):
-- sp_GetDashboard.sql
-- sp_GetAdminGroupStats.sql
-- sp_GetAdminAttendanceToday.sql
-- sp_GetAdminAlerts.sql
```

**5. Сборка и запуск**

```
1. Откройте CollegeJournalApp.sln в Visual Studio
2. Правой кнопкой → Restore NuGet Packages
3. F5 или Ctrl+F5 для запуска
```

---

## 10. Тестовые учётные записи

| Роль | Логин | Пароль | ФИО |
|------|-------|--------|-----|
| Admin | `admin` | `admin123` | Администратор Системы |
| Teacher | `teacher1` | `pass123` | Петров Пётр Петрович |
| Curator | `curator1` | `pass123` | Иванова Ирина Ивановна |
| Headman | `headman1` | `pass123` | Сидоров Сидор Сидорович |
| Student | `student1` | `pass123` | Козлов Алексей Викторович |

> Пароли хранятся в виде хеша SHA2_256. Изменить пароль можно через Admin-панель → Пользователи.

---

## 11. Архитектура и поток данных

```
┌─────────────────────────────────────────────────────────┐
│                    WPF UI Layer                          │
│  LoginWindow → MainWindow (Frame) → Pages               │
│                                                          │
│  DashboardPage  GradesPage  AttendancePage  ...          │
│       │               │           │                      │
│  Code-Behind   Code-Behind  Code-Behind                  │
└────────────────────────┬────────────────────────────────┘
                         │ SqlParameter[]
                         ▼
┌─────────────────────────────────────────────────────────┐
│                  DatabaseHelper                          │
│   ExecuteProcedure() → DataTable                        │
│   ExecuteNonQuery()  → int (rows affected)              │
└────────────────────────┬────────────────────────────────┘
                         │ ADO.NET / Microsoft.Data.SqlClient
                         ▼
┌─────────────────────────────────────────────────────────┐
│              SQL Server (CollegeJournal DB)              │
│                                                          │
│   Stored Procedures → Tables → Constraints              │
└─────────────────────────────────────────────────────────┘
```

### Паттерны, применяемые в проекте

| Паттерн | Где применяется |
|---------|----------------|
| **DataTable-пивот** | `GradesPage` — плоские SQL-строки → матрица студент×день |
| **Soft Delete** | Все основные таблицы — флаг `IsDeleted = 1` вместо DELETE |
| **UPSERT (MERGE)** | `sp_SaveLessonNote`, `sp_SaveAttendanceMark`, `sp_ImportScheduleItem` |
| **IValueConverter** | Конвертеры оценок → цвет/текст в DataGrid |
| **WPF Popup** | Попапы для ввода оценок и тем уроков |
| **SessionHelper (Singleton-state)** | Хранение данных текущего пользователя |
| **FrameworkElementFactory** | Динамическое создание колонок DataGrid с шаблонами |

---

## 12. Безопасность

- **Хранение паролей:** `HASHBYTES('SHA2_256', ...)` на стороне SQL Server — пароли не передаются в открытом виде
- **Ролевое разграничение:** Все страницы проверяют `SessionHelper.RoleName` при загрузке и скрывают/деактивируют элементы управления
- **Мягкое удаление:** Данные не удаляются физически; восстановление возможно на уровне БД
- **Аудит:** Все значимые действия записываются в `dbo.AuditLog` с UserId, типом действия и временной меткой
- **Хранимые процедуры:** Весь доступ к данным — через SP, прямые запросы из кода отсутствуют (защита от SQL-инъекций)
- **Строка подключения:** Хранится в `App.config`, рекомендуется вынести в зашифрованную секцию для Production

---

*Документация актуальна для версии приложения от апреля 2026 г.*
