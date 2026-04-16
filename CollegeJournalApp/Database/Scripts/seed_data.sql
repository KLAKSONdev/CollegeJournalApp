USE CollegeJournal;
GO

-- ============================================================
-- Сброс IDENTITY_INSERT (SET OFF безопасен даже если уже OFF).
-- Нужно на случай если предыдущий запуск упал внутри транзакции
-- и оставил флаг включённым для одной из таблиц.
-- ============================================================
SET IDENTITY_INSERT dbo.Roles             OFF;
SET IDENTITY_INSERT dbo.Users             OFF;
SET IDENTITY_INSERT dbo.Dormitories       OFF;
SET IDENTITY_INSERT dbo.Groups            OFF;
SET IDENTITY_INSERT dbo.Teachers          OFF;
SET IDENTITY_INSERT dbo.Students          OFF;
SET IDENTITY_INSERT dbo.Subjects          OFF;
SET IDENTITY_INSERT dbo.Schedule          OFF;
SET IDENTITY_INSERT dbo.StudentSocialInfo OFF;
SET IDENTITY_INSERT dbo.Parents           OFF;
SET IDENTITY_INSERT dbo.Grades            OFF;
SET IDENTITY_INSERT dbo.Attendance        OFF;
SET IDENTITY_INSERT dbo.Achievements      OFF;
SET IDENTITY_INSERT dbo.Documents         OFF;
GO

-- ============================================================
-- seed_data.sql  — идемпотентный скрипт тестовых данных
-- Каждая строка вставляется только если её ещё нет.
-- Можно запускать повторно без ошибок.
-- Пароль для всех учётных записей: test123
-- ============================================================

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @pwd NVARCHAR(256) =
        LOWER(CONVERT(NVARCHAR(256), HASHBYTES('SHA2_256', N'test123'), 2));

    -- ============================================================
    -- AcademicYears — берём существующий или создаём
    -- ============================================================
    DECLARE @YearId INT;
    SELECT TOP 1 @YearId = YearId
    FROM dbo.AcademicYears
    WHERE IsDeleted = 0
    ORDER BY IsCurrent DESC, YearId DESC;

    IF @YearId IS NULL
    BEGIN
        INSERT INTO dbo.AcademicYears (Title, StartDate, EndDate, IsCurrent, IsDeleted)
        VALUES (N'2024–2025', '2024-09-01', '2025-06-30', 1, 0);
        SET @YearId = SCOPE_IDENTITY();
    END

    -- ============================================================
    -- Roles
    -- ============================================================
    SET IDENTITY_INSERT dbo.Roles ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleId = 1)
        INSERT INTO dbo.Roles (RoleId, RoleName, Description) VALUES
        (1, N'Admin',   N'Системный администратор — полный доступ ко всем функциям');
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleId = 2)
        INSERT INTO dbo.Roles (RoleId, RoleName, Description) VALUES
        (2, N'Curator', N'Куратор группы — просмотр и редактирование данных своей группы');
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleId = 3)
        INSERT INTO dbo.Roles (RoleId, RoleName, Description) VALUES
        (3, N'Headman', N'Cтароста группы — ограниченный доступ к данным своей группы');
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleId = 4)
        INSERT INTO dbo.Roles (RoleId, RoleName, Description) VALUES
        (4, N'Student', N'Студент — просмотр собственных оценок и расписания');
    SET IDENTITY_INSERT dbo.Roles OFF;

    -- ============================================================
    -- Users (11 строк, каждая — отдельный IF NOT EXISTS)
    -- ============================================================
    SET IDENTITY_INSERT dbo.Users ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 1  OR Login = N'admin')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (1,1,N'admin',@pwd,N'Администратов',N'Админ',N'Системович',N'+79001000001',N'admin@college.ru',1,'2024-08-15 09:00:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 2  OR Login = N'ivanova_m')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (2,2,N'ivanova_m',@pwd,N'Иванова',N'Мария',N'Петровна',N'+79001000002',N'ivanova.m@college.ru',1,'2024-08-15 09:05:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 3  OR Login = N'petrov_a')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (3,2,N'petrov_a',@pwd,N'Петров',N'Алексей',N'Николаевич',N'+79001000003',N'petrov.a@college.ru',1,'2024-08-15 09:10:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 4  OR Login = N'smirnov_d')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (4,3,N'smirnov_d',@pwd,N'Смирнов',N'Дмитрий',N'Андреевич',N'+79001000004',N'smirnov.d@college.ru',1,'2024-08-20 10:00:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 5  OR Login = N'morozov_n')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (5,3,N'morozov_n',@pwd,N'Морозов',N'Никита',N'Евгеньевич',N'+79001000005',N'morozov.n@college.ru',1,'2024-08-20 10:05:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 6  OR Login = N'kozlova_a')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (6,4,N'kozlova_a',@pwd,N'Козлова',N'Анастасия',N'Игоревна',N'+79001000006',N'kozlova.a@college.ru',1,'2024-08-20 10:10:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 7  OR Login = N'novikov_ar')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (7,4,N'novikov_ar',@pwd,N'Новиков',N'Артём',N'Владимирович',N'+79001000007',N'novikov.ar@college.ru',1,'2024-08-20 10:15:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 8  OR Login = N'lebedeva_o')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (8,4,N'lebedeva_o',@pwd,N'Лебедева',N'Ольга',N'Сергеевна',N'+79001000008',N'lebedeva.o@college.ru',1,'2024-08-20 10:20:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 9  OR Login = N'zaitseva_d')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (9,4,N'zaitseva_d',@pwd,N'Зайцева',N'Дарья',N'Алексеевна',N'+79001000009',N'zaitseva.d@college.ru',1,'2024-08-20 10:25:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 10 OR Login = N'orlov_m')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (10,4,N'orlov_m',@pwd,N'Орлов',N'Максим',N'Дмитриевич',N'+79001000010',N'orlov.m@college.ru',1,'2024-08-20 10:30:00',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 11 OR Login = N'sokolova_v')
        INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted)
        VALUES (11,4,N'sokolova_v',@pwd,N'Соколова',N'Виктория',N'Романовна',N'+79001000011',N'sokolova.v@college.ru',1,'2024-08-20 10:35:00',0);
    SET IDENTITY_INSERT dbo.Users OFF;

    -- ============================================================
    -- Dormitories
    -- ============================================================
    SET IDENTITY_INSERT dbo.Dormitories ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Dormitories WHERE DormitoryId = 1)
        INSERT INTO dbo.Dormitories (DormitoryId,Name,Address,CommandantName,Phone,TotalRooms,IsDeleted)
        VALUES (1,N'Общежитие №1',N'ул. Студенческая, д. 5',N'Громова Тамара Ивановна',N'+74951234501',120,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Dormitories WHERE DormitoryId = 2)
        INSERT INTO dbo.Dormitories (DormitoryId,Name,Address,CommandantName,Phone,TotalRooms,IsDeleted)
        VALUES (2,N'Общежитие №2',N'ул. Учебная, д. 12',N'Васильев Сергей Петрович',N'+74951234502',90,0);
    SET IDENTITY_INSERT dbo.Dormitories OFF;

    -- ============================================================
    -- Groups
    -- ============================================================
    SET IDENTITY_INSERT dbo.Groups ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Groups WHERE GroupId = 1)
        INSERT INTO dbo.Groups (GroupId,YearId,CuratorId,GroupName,Specialty,SpecialtyCode,Course,Semester,EducationForm,EducationBasis,StudentCount,IsGraduated,GraduationDate,IsDeleted)
        VALUES (1,@YearId,2,N'ЭБ-31',N'Экономика и бухгалтерский учёт',N'38.02.01',3,5,N'Очная',N'Бюджет',4,0,NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Groups WHERE GroupId = 2)
        INSERT INTO dbo.Groups (GroupId,YearId,CuratorId,GroupName,Specialty,SpecialtyCode,Course,Semester,EducationForm,EducationBasis,StudentCount,IsGraduated,GraduationDate,IsDeleted)
        VALUES (2,@YearId,3,N'ИС-21',N'Информационные системы и программирование',N'09.02.07',2,3,N'Очная',N'Смешанное',4,0,NULL,0);
    SET IDENTITY_INSERT dbo.Groups OFF;

    -- ============================================================
    -- Teachers (пропускаем существующие)
    -- ============================================================
    SET IDENTITY_INSERT dbo.Teachers ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE TeacherId = 1)
        INSERT INTO dbo.Teachers (TeacherId,LastName,FirstName,MiddleName,IsDeleted) VALUES (1,N'Кузнецова',N'Елена',N'Владимировна',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE TeacherId = 2)
        INSERT INTO dbo.Teachers (TeacherId,LastName,FirstName,MiddleName,IsDeleted) VALUES (2,N'Фёдоров',N'Игорь',N'Михайлович',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE TeacherId = 3)
        INSERT INTO dbo.Teachers (TeacherId,LastName,FirstName,MiddleName,IsDeleted) VALUES (3,N'Белова',N'Наталья',N'Юрьевна',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE TeacherId = 4)
        INSERT INTO dbo.Teachers (TeacherId,LastName,FirstName,MiddleName,IsDeleted) VALUES (4,N'Тихонов',N'Андрей',N'Сергеевич',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Teachers WHERE TeacherId = 5)
        INSERT INTO dbo.Teachers (TeacherId,LastName,FirstName,MiddleName,IsDeleted) VALUES (5,N'Соловьёва',N'Светлана',N'Александровна',0);
    SET IDENTITY_INSERT dbo.Teachers OFF;

    -- ============================================================
    -- Students
    -- ============================================================
    SET IDENTITY_INSERT dbo.Students ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = 1)
        INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted)
        VALUES (1,4,1,1,1,N'ЭБ-31-001','2004-03-12',N'г. Москва',N'Мужской',N'Россия',N'г. Москва, ул. Ленина, д. 10, кв. 5',N'001-002-003 04',N'4516',N'123456',N'ОВД Советского района г. Москвы','2020-03-12',N'ГБОУ СОШ №45 г. Москвы',N'Средняя школа',N'Бюджет',N'101','2022-09-01',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = 2)
        INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted)
        VALUES (2,6,1,1,0,N'ЭБ-31-002','2004-07-25',N'г. Москва',N'Женский',N'Россия',N'г. Москва, пр. Мира, д. 22, кв. 14',N'002-003-004 05',N'4517',N'234567',N'ОВД Центрального района г. Москвы','2020-07-25',N'ГБОУ СОШ №112 г. Москвы',N'Средняя школа',N'Бюджет',N'102','2022-09-01',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = 3)
        INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted)
        VALUES (3,7,1,NULL,0,N'ЭБ-31-003','2004-11-08',N'г. Подольск',N'Мужской',N'Россия',N'г. Подольск, ул. Садовая, д. 3, кв. 7',N'003-004-005 06',N'4518',N'345678',N'ОВД г. Подольска','2020-11-08',N'МОУ СОШ №5 г. Подольска',N'Средняя школа',N'Бюджет',NULL,'2022-09-01',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = 4)
        INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted)
        VALUES (4,8,1,2,0,N'ЭБ-31-004','2005-02-14',N'г. Балашиха',N'Женский',N'Россия',N'г. Балашиха, ул. Первомайская, д. 18, кв. 3',N'004-005-006 07',N'4519',N'456789',N'ОВД г. Балашихи','2021-02-14',N'МОУ СОШ №2 г. Балашихи',N'Средняя школа',N'Внебюджет',N'215','2022-09-01',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = 5)
        INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted)
        VALUES (5,5,2,NULL,1,N'ИС-21-001','2005-05-20',N'г. Москва',N'Мужской',N'Россия',N'г. Москва, ул. Гагарина, д. 7, кв. 11',N'005-006-007 08',N'4520',N'567890',N'ОВД Южного района г. Москвы','2021-05-20',N'ГБОУ СОШ №78 г. Москвы',N'Средняя школа',N'Внебюджет',NULL,'2023-09-01',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = 6)
        INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted)
        VALUES (6,9,2,2,0,N'ИС-21-002','2005-09-03',N'г. Химки',N'Женский',N'Россия',N'г. Химки, ул. Молодёжная, д. 5, кв. 9',N'006-007-008 09',N'4521',N'678901',N'ОВД г. Химки','2021-09-03',N'МОУ СОШ №7 г. Химки',N'Средняя школа',N'Бюджет',N'312','2023-09-01',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = 7)
        INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted)
        VALUES (7,10,2,NULL,0,N'ИС-21-003','2005-12-17',N'г. Мытищи',N'Мужской',N'Россия',N'г. Мытищи, пр. Олимпийский, д. 14, кв. 22',N'007-008-009 10',N'4522',N'789012',N'ОВД г. Мытищи','2021-12-17',N'МОУ СОШ №3 г. Мытищи',N'Средняя школа',N'Бюджет',NULL,'2023-09-01',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = 8)
        INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted)
        VALUES (8,11,2,1,0,N'ИС-21-004','2005-04-29',N'г. Люберцы',N'Женский',N'Россия',N'г. Люберцы, ул. Октябрьская, д. 30, кв. 6',N'008-009-010 11',N'4523',N'890123',N'ОВД г. Люберцы','2021-04-29',N'МОУ СОШ №11 г. Люберцы',N'Средняя школа',N'Бюджет',N'105','2023-09-01',0);
    SET IDENTITY_INSERT dbo.Students OFF;

    -- ============================================================
    -- Subjects (4 per group)
    -- ============================================================
    SET IDENTITY_INSERT dbo.Subjects ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectId = 1)
        INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted)
        VALUES (1,1,1,N'Бухгалтерский учёт и отчётность',72,24,32,0,16,N'5',N'Экзамен',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectId = 2)
        INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted)
        VALUES (2,1,3,N'Налоги и налогообложение',54,18,28,0,8,N'5',N'Зачёт',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectId = 3)
        INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted)
        VALUES (3,1,1,N'Финансы организаций',60,20,28,0,12,N'5',N'Экзамен',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectId = 4)
        INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted)
        VALUES (4,1,5,N'Экономический анализ',54,18,24,0,12,N'5',N'Зачёт',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectId = 5)
        INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted)
        VALUES (5,2,2,N'Основы программирования',90,30,20,30,10,N'3',N'Экзамен',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectId = 6)
        INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted)
        VALUES (6,2,4,N'Базы данных',72,24,16,24,8,N'3',N'Экзамен',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectId = 7)
        INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted)
        VALUES (7,2,2,N'Веб-технологии',60,20,12,20,8,N'3',N'Зачёт',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE SubjectId = 8)
        INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted)
        VALUES (8,2,4,N'Операционные системы',54,18,12,18,6,N'3',N'Зачёт',0);
    SET IDENTITY_INSERT dbo.Subjects OFF;

    -- ============================================================
    -- Schedule (6 per group)
    -- ============================================================
    SET IDENTITY_INSERT dbo.Schedule ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 1)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (1,1,1,1,1,N'ауд. 201',N'Обе','08:30','10:05',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 2)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (2,1,2,1,2,N'ауд. 203',N'Обе','10:15','11:50',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 3)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (3,1,3,2,1,N'ауд. 201',N'Обе','08:30','10:05',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 4)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (4,1,4,2,2,N'ауд. 115',N'Обе','10:15','11:50',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 5)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (5,1,1,3,3,N'ауд. 201',N'Обе','12:30','14:05',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 6)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (6,1,3,4,1,N'ауд. 203',N'Обе','08:30','10:05',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 7)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (7,2,5,1,3,N'ауд. 301',N'Обе','12:30','14:05',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 8)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (8,2,6,2,3,N'ауд. 307',N'Обе','12:30','14:05',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 9)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (9,2,5,3,1,N'ауд. 301',N'Обе','08:30','10:05',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 10)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (10,2,7,3,2,N'ауд. 302',N'Обе','10:15','11:50',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 11)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (11,2,8,4,2,N'ауд. 305',N'Обе','10:15','11:50',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Schedule WHERE ScheduleId = 12)
        INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted)
        VALUES (12,2,6,5,4,N'ауд. 307',N'Обе','14:15','15:50',0);
    SET IDENTITY_INSERT dbo.Schedule OFF;

    -- ============================================================
    -- StudentSocialInfo
    -- ============================================================
    SET IDENTITY_INSERT dbo.StudentSocialInfo ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.StudentSocialInfo WHERE SocialInfoId = 1)
        INSERT INTO dbo.StudentSocialInfo (SocialInfoId,StudentId,HealthGroup,ChronicDiseases,Disability,DisabilityGroup,DisabilityCertificate,IsOrphan,IsHalfOrphan,IsFromLargeFamily,IsLowIncome,IsSociallyVulnerable,IsOnGuardianship,SocialBenefits,PsychologicalFeatures,HousingCondition,FamilyStructure,FamilyType,AdditionalNotes)
        VALUES (1,1,N'Основная',NULL,NULL,NULL,NULL,0,0,0,0,0,0,NULL,NULL,N'Собственное жильё',N'Полная семья (отец, мать)',N'Благополучная',NULL);
    IF NOT EXISTS (SELECT 1 FROM dbo.StudentSocialInfo WHERE SocialInfoId = 2)
        INSERT INTO dbo.StudentSocialInfo (SocialInfoId,StudentId,HealthGroup,ChronicDiseases,Disability,DisabilityGroup,DisabilityCertificate,IsOrphan,IsHalfOrphan,IsFromLargeFamily,IsLowIncome,IsSociallyVulnerable,IsOnGuardianship,SocialBenefits,PsychologicalFeatures,HousingCondition,FamilyStructure,FamilyType,AdditionalNotes)
        VALUES (2,2,N'Подготовительная',N'Аллергический ринит',NULL,NULL,NULL,0,1,0,1,0,0,N'Социальная стипендия',N'Повышенная тревожность',N'Общежитие',N'Неполная семья (мать)',N'Малообеспеченная',N'Получает социальную поддержку по потере кормильца');
    IF NOT EXISTS (SELECT 1 FROM dbo.StudentSocialInfo WHERE SocialInfoId = 3)
        INSERT INTO dbo.StudentSocialInfo (SocialInfoId,StudentId,HealthGroup,ChronicDiseases,Disability,DisabilityGroup,DisabilityCertificate,IsOrphan,IsHalfOrphan,IsFromLargeFamily,IsLowIncome,IsSociallyVulnerable,IsOnGuardianship,SocialBenefits,PsychologicalFeatures,HousingCondition,FamilyStructure,FamilyType,AdditionalNotes)
        VALUES (3,3,N'Основная',NULL,NULL,NULL,NULL,0,0,1,0,0,0,N'Льгота многодетной семьи',NULL,N'Собственное жильё',N'Полная семья (отец, мать, 3 детей)',N'Многодетная',NULL);
    IF NOT EXISTS (SELECT 1 FROM dbo.StudentSocialInfo WHERE SocialInfoId = 4)
        INSERT INTO dbo.StudentSocialInfo (SocialInfoId,StudentId,HealthGroup,ChronicDiseases,Disability,DisabilityGroup,DisabilityCertificate,IsOrphan,IsHalfOrphan,IsFromLargeFamily,IsLowIncome,IsSociallyVulnerable,IsOnGuardianship,SocialBenefits,PsychologicalFeatures,HousingCondition,FamilyStructure,FamilyType,AdditionalNotes)
        VALUES (4,5,N'Специальная',N'Сахарный диабет II типа',N'Ограниченные возможности здоровья (ОВЗ)',N'Нет группы',N'МСЭ-2021-001234',0,0,0,0,1,0,N'Повышенная стипендия, бесплатное питание',N'Требует индивидуального сопровождения',N'Собственное жильё',N'Полная семья (отец, мать)',N'Благополучная',N'Требуется контроль уровня сахара');
    IF NOT EXISTS (SELECT 1 FROM dbo.StudentSocialInfo WHERE SocialInfoId = 5)
        INSERT INTO dbo.StudentSocialInfo (SocialInfoId,StudentId,HealthGroup,ChronicDiseases,Disability,DisabilityGroup,DisabilityCertificate,IsOrphan,IsHalfOrphan,IsFromLargeFamily,IsLowIncome,IsSociallyVulnerable,IsOnGuardianship,SocialBenefits,PsychologicalFeatures,HousingCondition,FamilyStructure,FamilyType,AdditionalNotes)
        VALUES (5,6,N'Основная',NULL,NULL,NULL,NULL,0,0,0,0,0,0,NULL,NULL,N'Общежитие',N'Полная семья (отец, мать, брат)',N'Благополучная',NULL);
    SET IDENTITY_INSERT dbo.StudentSocialInfo OFF;

    -- ============================================================
    -- Parents
    -- ============================================================
    SET IDENTITY_INSERT dbo.Parents ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Parents WHERE ParentId = 1)
        INSERT INTO dbo.Parents (ParentId,StudentId,Relation,LastName,FirstName,MiddleName,BirthDate,Phone,WorkPhone,Email,Address,Workplace,Position,Department,Education,IsMainContact,IsDeceased,HasParentalRights,IsDeleted)
        VALUES (1,1,N'Отец',N'Смирнов',N'Андрей',N'Викторович','1975-06-15',N'+79001100001',N'+74951110001',N'smirnov.av@mail.ru',N'г. Москва, ул. Ленина, д. 10, кв. 5',N'ООО «СтройГрупп»',N'Инженер',N'Производственный отдел',N'Высшее техническое',1,0,1,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Parents WHERE ParentId = 2)
        INSERT INTO dbo.Parents (ParentId,StudentId,Relation,LastName,FirstName,MiddleName,BirthDate,Phone,WorkPhone,Email,Address,Workplace,Position,Department,Education,IsMainContact,IsDeceased,HasParentalRights,IsDeleted)
        VALUES (2,1,N'Мать',N'Смирнова',N'Ольга',N'Николаевна','1978-09-22',N'+79001100002',N'+74951110002',N'smirnova.on@mail.ru',N'г. Москва, ул. Ленина, д. 10, кв. 5',N'ГБУЗ «Городская больница №5»',N'Врач-терапевт',N'Терапевтическое отделение',N'Высшее медицинское',0,0,1,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Parents WHERE ParentId = 3)
        INSERT INTO dbo.Parents (ParentId,StudentId,Relation,LastName,FirstName,MiddleName,BirthDate,Phone,WorkPhone,Email,Address,Workplace,Position,Department,Education,IsMainContact,IsDeceased,HasParentalRights,IsDeleted)
        VALUES (3,2,N'Мать',N'Козлова',N'Светлана',N'Ивановна','1980-03-08',N'+79001100003',N'+74951110003',N'kozlova.si@mail.ru',N'г. Москва, пр. Мира, д. 22, кв. 14',N'МБОУ СОШ №112',N'Учитель начальных классов',N'Начальная школа',N'Высшее педагогическое',1,0,1,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Parents WHERE ParentId = 4)
        INSERT INTO dbo.Parents (ParentId,StudentId,Relation,LastName,FirstName,MiddleName,BirthDate,Phone,WorkPhone,Email,Address,Workplace,Position,Department,Education,IsMainContact,IsDeceased,HasParentalRights,IsDeleted)
        VALUES (4,3,N'Отец',N'Новиков',N'Владимир',N'Степанович','1972-11-30',N'+79001100004',N'+74951110004',N'novikov.vs@mail.ru',N'г. Подольск, ул. Садовая, д. 3, кв. 7',N'МУП «Подольскводоканал»',N'Слесарь-сантехник',N'Эксплуатационный отдел',N'Среднее профессиональное',1,0,1,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Parents WHERE ParentId = 5)
        INSERT INTO dbo.Parents (ParentId,StudentId,Relation,LastName,FirstName,MiddleName,BirthDate,Phone,WorkPhone,Email,Address,Workplace,Position,Department,Education,IsMainContact,IsDeceased,HasParentalRights,IsDeleted)
        VALUES (5,4,N'Мать',N'Лебедева',N'Татьяна',N'Александровна','1982-07-19',N'+79001100005',N'+74951110005',N'lebedeva.ta@mail.ru',N'г. Балашиха, ул. Первомайская, д. 18, кв. 3',N'ПАО «Балашихинский завод»',N'Бухгалтер',N'Финансовый отдел',N'Высшее экономическое',1,0,1,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Parents WHERE ParentId = 6)
        INSERT INTO dbo.Parents (ParentId,StudentId,Relation,LastName,FirstName,MiddleName,BirthDate,Phone,WorkPhone,Email,Address,Workplace,Position,Department,Education,IsMainContact,IsDeceased,HasParentalRights,IsDeleted)
        VALUES (6,5,N'Отец',N'Морозов',N'Евгений',N'Павлович','1976-04-05',N'+79001100006',N'+74951110006',N'morozov.ep@mail.ru',N'г. Москва, ул. Гагарина, д. 7, кв. 11',N'АО «ИнфоТех»',N'Системный администратор',N'IT-отдел',N'Высшее техническое',1,0,1,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Parents WHERE ParentId = 7)
        INSERT INTO dbo.Parents (ParentId,StudentId,Relation,LastName,FirstName,MiddleName,BirthDate,Phone,WorkPhone,Email,Address,Workplace,Position,Department,Education,IsMainContact,IsDeceased,HasParentalRights,IsDeleted)
        VALUES (7,5,N'Мать',N'Морозова',N'Ирина',N'Сергеевна','1979-12-10',N'+79001100007',N'+74951110007',N'morozova.is@mail.ru',N'г. Москва, ул. Гагарина, д. 7, кв. 11',N'ГБОУ СОШ №78',N'Преподаватель математики',N'Отдел точных наук',N'Высшее педагогическое',0,0,1,0);
    SET IDENTITY_INSERT dbo.Parents OFF;

    -- ============================================================
    -- Grades (24 записи)
    -- ============================================================
    SET IDENTITY_INSERT dbo.Grades ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 1)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (1,1,1,2,5,'2025-01-14',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 2)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (2,1,2,2,4,'2025-01-28',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 3)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (3,1,3,2,5,'2025-02-11',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 4)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (4,2,1,2,4,'2025-01-14',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 5)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (5,2,2,2,3,'2025-01-28',N'Текущая',N'Необходимо улучшить знания',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 6)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (6,2,4,2,4,'2025-02-18',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 7)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (7,3,1,2,3,'2025-01-21',N'Текущая',N'Требуется доработка',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 8)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (8,3,3,2,4,'2025-02-04',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 9)  INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (9,3,4,2,4,'2025-03-04',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 10) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (10,4,1,2,4,'2025-01-21',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 11) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (11,4,2,2,5,'2025-02-04',N'Текущая',N'Отличный результат',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 12) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (12,4,3,2,4,'2025-03-11',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 13) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (13,5,5,3,5,'2025-01-15',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 14) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (14,5,6,3,5,'2025-02-05',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 15) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (15,5,7,3,4,'2025-03-05',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 16) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (16,6,5,3,4,'2025-01-15',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 17) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (17,6,6,3,3,'2025-02-05',N'Текущая',N'Нужно повторить тему JOIN',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 18) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (18,6,8,3,4,'2025-03-19',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 19) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (19,7,5,3,3,'2025-01-22',N'Текущая',N'Сдан со второй попытки',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 20) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (20,7,7,3,4,'2025-02-12',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 21) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (21,7,8,3,4,'2025-03-26',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 22) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (22,8,5,3,5,'2025-01-22',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 23) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (23,8,6,3,4,'2025-02-19',N'Текущая',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Grades WHERE GradeId = 24) INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES (24,8,7,3,5,'2025-04-02',N'Текущая',N'Лучший результат в группе',0);
    SET IDENTITY_INSERT dbo.Grades OFF;

    -- ============================================================
    -- Attendance (20 записей, апрель 2025)
    -- ============================================================
    SET IDENTITY_INSERT dbo.Attendance ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 1)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (1,1,1,2,'2025-04-07',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 2)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (2,2,1,2,'2025-04-07',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 3)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (3,3,1,2,'2025-04-07',N'Отсутствовал',N'Болезнь (справка)',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 4)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (4,4,1,2,'2025-04-07',N'Опоздал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 5)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (5,1,3,2,'2025-04-08',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 6)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (6,2,3,2,'2025-04-08',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 7)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (7,3,3,2,'2025-04-08',N'Отсутствовал',N'Болезнь (справка)',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 8)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (8,4,3,2,'2025-04-08',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 9)  INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (9,1,5,2,'2025-04-09',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 10) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (10,2,5,2,'2025-04-09',N'Опоздал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 11) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (11,5,7,3,'2025-04-07',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 12) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (12,6,7,3,'2025-04-07',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 13) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (13,7,7,3,'2025-04-07',N'Отсутствовал',N'Семейные обстоятельства',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 14) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (14,8,7,3,'2025-04-07',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 15) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (15,5,8,3,'2025-04-08',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 16) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (16,6,8,3,'2025-04-08',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 17) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (17,7,8,3,'2025-04-08',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 18) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (18,8,8,3,'2025-04-08',N'Опоздал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 19) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (19,5,9,3,'2025-04-09',N'Присутствовал',NULL,0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Attendance WHERE AttendanceId = 20) INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES (20,6,9,3,'2025-04-09',N'Отсутствовал',N'Участие в соревнованиях',0);
    SET IDENTITY_INSERT dbo.Attendance OFF;

    -- ============================================================
    -- Achievements
    -- ============================================================
    SET IDENTITY_INSERT dbo.Achievements ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Achievements WHERE AchievementId = 1)
        INSERT INTO dbo.Achievements (AchievementId,StudentId,AddedById,Title,Category,Level,Description,AchieveDate,DocumentNumber,IsDeleted)
        VALUES (1,1,2,N'Победитель олимпиады по бухгалтерскому учёту',N'Академическая',N'Региональный',N'1 место в региональной олимпиаде по бухгалтерскому учёту среди СПО','2025-02-20',N'ОЛ-2025-0045',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Achievements WHERE AchievementId = 2)
        INSERT INTO dbo.Achievements (AchievementId,StudentId,AddedById,Title,Category,Level,Description,AchieveDate,DocumentNumber,IsDeleted)
        VALUES (2,1,2,N'Лучший студент семестра',N'Академическая',N'Учреждение',N'Признан лучшим студентом факультета по итогам 5 семестра','2025-01-31',N'ПР-2025-0012',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Achievements WHERE AchievementId = 3)
        INSERT INTO dbo.Achievements (AchievementId,StudentId,AddedById,Title,Category,Level,Description,AchieveDate,DocumentNumber,IsDeleted)
        VALUES (3,4,2,N'Участник конференции «Молодой экономист»',N'Научная',N'Региональный',N'Выступила с докладом на тему «Налоговое планирование в малом бизнесе»','2025-03-15',N'КОН-2025-0089',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Achievements WHERE AchievementId = 4)
        INSERT INTO dbo.Achievements (AchievementId,StudentId,AddedById,Title,Category,Level,Description,AchieveDate,DocumentNumber,IsDeleted)
        VALUES (4,5,3,N'Победитель хакатона по программированию',N'Профессиональная',N'Региональный',N'1 место в командном хакатоне «IT-старт», разработка мобильного приложения','2025-03-28',N'ХАК-2025-0017',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Achievements WHERE AchievementId = 5)
        INSERT INTO dbo.Achievements (AchievementId,StudentId,AddedById,Title,Category,Level,Description,AchieveDate,DocumentNumber,IsDeleted)
        VALUES (5,5,3,N'Стипендия губернатора',N'Академическая',N'Региональный',N'Назначена именная стипендия губернатора за высокие академические результаты','2025-02-01',N'СТИ-2025-0003',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Achievements WHERE AchievementId = 6)
        INSERT INTO dbo.Achievements (AchievementId,StudentId,AddedById,Title,Category,Level,Description,AchieveDate,DocumentNumber,IsDeleted)
        VALUES (6,8,3,N'Призёр конкурса веб-разработки',N'Профессиональная',N'Региональный',N'3 место на региональном конкурсе «Лучший сайт» в категории СПО','2025-04-05',N'КОН-2025-0102',0);
    SET IDENTITY_INSERT dbo.Achievements OFF;

    -- ============================================================
    -- Documents
    -- ============================================================
    SET IDENTITY_INSERT dbo.Documents ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Documents WHERE DocumentId = 1)
        INSERT INTO dbo.Documents (DocumentId,GroupId,UploadedById,Title,DocumentType,FilePath,FileSize,UploadedAt,Description,IsDeleted)
        VALUES (1,1,2,N'Учебный план ЭБ-31 на 2024-2025',N'Учебный план',N'/documents/groups/1/plan_eb31_2024_2025.pdf',N'512 KB','2024-09-02 10:00:00',N'Утверждённый учебный план группы на текущий год',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Documents WHERE DocumentId = 2)
        INSERT INTO dbo.Documents (DocumentId,GroupId,UploadedById,Title,DocumentType,FilePath,FileSize,UploadedAt,Description,IsDeleted)
        VALUES (2,1,2,N'Приказ о зачислении ЭБ-31',N'Приказ',N'/documents/groups/1/order_enroll_eb31.pdf',N'128 KB','2024-09-02 10:15:00',N'Приказ о зачислении студентов группы ЭБ-31',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Documents WHERE DocumentId = 3)
        INSERT INTO dbo.Documents (DocumentId,GroupId,UploadedById,Title,DocumentType,FilePath,FileSize,UploadedAt,Description,IsDeleted)
        VALUES (3,2,3,N'Учебный план ИС-21 на 2024-2025',N'Учебный план',N'/documents/groups/2/plan_is21_2024_2025.pdf',N'498 KB','2024-09-02 11:00:00',N'Утверждённый учебный план группы на текущий год',0);
    IF NOT EXISTS (SELECT 1 FROM dbo.Documents WHERE DocumentId = 4)
        INSERT INTO dbo.Documents (DocumentId,GroupId,UploadedById,Title,DocumentType,FilePath,FileSize,UploadedAt,Description,IsDeleted)
        VALUES (4,2,3,N'Приказ о зачислении ИС-21',N'Приказ',N'/documents/groups/2/order_enroll_is21.pdf',N'134 KB','2024-09-02 11:20:00',N'Приказ о зачислении студентов группы ИС-21',0);
    SET IDENTITY_INSERT dbo.Documents OFF;

    COMMIT TRANSACTION;

    PRINT N'';
    PRINT N'=======================================================';
    PRINT N'  Тестовые данные успешно загружены!';
    PRINT N'=======================================================';
    PRINT N'';
    PRINT N'  Пароль для всех учётных записей: test123';
    PRINT N'';
    PRINT N'  Роль       Логин          Пользователь';
    PRINT N'  ---------- -------------- --------------------------------';
    PRINT N'  Admin      admin          Администратов Админ Системович';
    PRINT N'  Curator    ivanova_m      Иванова Мария Петровна (ЭБ-31)';
    PRINT N'  Curator    petrov_a       Петров Алексей Николаевич (ИС-21)';
    PRINT N'  Headman    smirnov_d      Смирнов Дмитрий Андреевич (ЭБ-31)';
    PRINT N'  Headman    morozov_n      Морозов Никита Евгеньевич (ИС-21)';
    PRINT N'  Student    kozlova_a      Козлова Анастасия Игоревна';
    PRINT N'  Student    novikov_ar     Новиков Артём Владимирович';
    PRINT N'  Student    lebedeva_o     Лебедева Ольга Сергеевна';
    PRINT N'  Student    zaitseva_d     Зайцева Дарья Алексеевна';
    PRINT N'  Student    orlov_m        Орлов Максим Дмитриевич';
    PRINT N'  Student    sokolova_v     Соколова Виктория Романовна';
    PRINT N'';
    PRINT N'=======================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT N'';
    PRINT N'ОШИБКА при загрузке тестовых данных!';
    PRINT N'Номер ошибки : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    PRINT N'Строка       : ' + CAST(ERROR_LINE()   AS NVARCHAR(10));
    PRINT N'Сообщение    : ' + ERROR_MESSAGE();
    PRINT N'Транзакция отменена.';
END CATCH;
GO
