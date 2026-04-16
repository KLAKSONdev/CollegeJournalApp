USE CollegeJournal;
GO

SET IDENTITY_INSERT dbo.AcademicYears     OFF;
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

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @pwd NVARCHAR(256) =
        LOWER(CONVERT(NVARCHAR(256), HASHBYTES('SHA2_256', N'test123'), 2));

    -- -------------------------------------------------------
    -- AcademicYears
    -- CK_AcademicYears_Title: '[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]'
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.AcademicYears ON;
    INSERT INTO dbo.AcademicYears (YearId,Title,StartDate,EndDate,IsCurrent,IsDeleted)
    VALUES (1,N'2024-2025','2024-09-01','2025-06-30',1,0);
    SET IDENTITY_INSERT dbo.AcademicYears OFF;

    -- -------------------------------------------------------
    -- Roles
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Roles ON;
    INSERT INTO dbo.Roles (RoleId,RoleName,Description) VALUES
    (1,N'Admin',   N'Системный администратор — полный доступ ко всем функциям'),
    (2,N'Curator', N'Куратор группы — просмотр и редактирование данных своей группы'),
    (3,N'Headman', N'Cтароста группы — ограниченный доступ к данным своей группы'),
    (4,N'Student', N'Студент — просмотр собственных оценок и расписания');
    SET IDENTITY_INSERT dbo.Roles OFF;

    -- -------------------------------------------------------
    -- Users
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Users ON;
    INSERT INTO dbo.Users (UserId,RoleId,Login,PasswordHash,LastName,FirstName,MiddleName,Phone,Email,IsActive,CreatedAt,IsDeleted) VALUES
    (1,  1,N'admin',      @pwd,N'Администратов',N'Админ',    N'Системович',  N'+79001000001',N'admin@college.ru',      1,'2024-08-15 09:00:00',0),
    (2,  2,N'ivanova_m',  @pwd,N'Иванова',      N'Мария',    N'Петровна',    N'+79001000002',N'ivanova.m@college.ru',  1,'2024-08-15 09:05:00',0),
    (3,  2,N'petrov_a',   @pwd,N'Петров',       N'Алексей',  N'Николаевич',  N'+79001000003',N'petrov.a@college.ru',   1,'2024-08-15 09:10:00',0),
    (4,  3,N'smirnov_d',  @pwd,N'Смирнов',      N'Дмитрий',  N'Андреевич',   N'+79001000004',N'smirnov.d@college.ru',  1,'2024-08-20 10:00:00',0),
    (5,  3,N'morozov_n',  @pwd,N'Морозов',      N'Никита',   N'Евгеньевич',  N'+79001000005',N'morozov.n@college.ru',  1,'2024-08-20 10:05:00',0),
    (6,  4,N'kozlova_a',  @pwd,N'Козлова',      N'Анастасия',N'Игоревна',    N'+79001000006',N'kozlova.a@college.ru',  1,'2024-08-20 10:10:00',0),
    (7,  4,N'novikov_ar', @pwd,N'Новиков',      N'Артём',    N'Владимирович',N'+79001000007',N'novikov.ar@college.ru', 1,'2024-08-20 10:15:00',0),
    (8,  4,N'lebedeva_o', @pwd,N'Лебедева',     N'Ольга',    N'Сергеевна',   N'+79001000008',N'lebedeva.o@college.ru', 1,'2024-08-20 10:20:00',0),
    (9,  4,N'zaitseva_d', @pwd,N'Зайцева',      N'Дарья',    N'Алексеевна',  N'+79001000009',N'zaitseva.d@college.ru', 1,'2024-08-20 10:25:00',0),
    (10, 4,N'orlov_m',    @pwd,N'Орлов',        N'Максим',   N'Дмитриевич',  N'+79001000010',N'orlov.m@college.ru',    1,'2024-08-20 10:30:00',0),
    (11, 4,N'sokolova_v', @pwd,N'Соколова',     N'Виктория', N'Романовна',   N'+79001000011',N'sokolova.v@college.ru', 1,'2024-08-20 10:35:00',0);
    SET IDENTITY_INSERT dbo.Users OFF;

    -- -------------------------------------------------------
    -- Dormitories
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Dormitories ON;
    INSERT INTO dbo.Dormitories (DormitoryId,Name,Address,CommandantName,Phone,TotalRooms,IsDeleted) VALUES
    (1,N'Общежитие №1',N'ул. Студенческая, д. 5',N'Громова Тамара Ивановна', N'+74951234501',120,0),
    (2,N'Общежитие №2',N'ул. Учебная, д. 12',    N'Васильев Сергей Петрович',N'+74951234502',90, 0);
    SET IDENTITY_INSERT dbo.Dormitories OFF;

    -- -------------------------------------------------------
    -- Groups
    -- CK_Groups_EduBasis: 'Бюджет' | 'Контракт' | 'Смешанная'
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Groups ON;
    INSERT INTO dbo.Groups (GroupId,YearId,CuratorId,GroupName,Specialty,SpecialtyCode,Course,Semester,EducationForm,EducationBasis,StudentCount,IsGraduated,GraduationDate,IsDeleted) VALUES
    (1,1,2,N'ЭБ-31',N'Экономика и бухгалтерский учёт',           N'38.02.01',3,5,N'Очная',N'Бюджет',   4,0,NULL,0),
    (2,1,3,N'ИС-21',N'Информационные системы и программирование', N'09.02.07',2,3,N'Очная',N'Смешанная',4,0,NULL,0);
    SET IDENTITY_INSERT dbo.Groups OFF;

    -- -------------------------------------------------------
    -- Teachers  (IsActive: DEFAULT=1)
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Teachers ON;
    INSERT INTO dbo.Teachers (TeacherId,LastName,FirstName,MiddleName,IsActive,IsDeleted) VALUES
    (1,N'Кузнецова',N'Елена',   N'Владимировна', 1,0),
    (2,N'Фёдоров',  N'Игорь',   N'Михайлович',   1,0),
    (3,N'Белова',   N'Наталья', N'Юрьевна',      1,0),
    (4,N'Тихонов',  N'Андрей',  N'Сергеевич',    1,0),
    (5,N'Соловьёва',N'Светлана',N'Александровна',1,0);
    SET IDENTITY_INSERT dbo.Teachers OFF;

    -- -------------------------------------------------------
    -- Students
    -- CK_Students_StudyBasis:    'Бюджет' | 'Контракт'
    -- CK_Students_PrevSchoolType: NULL | 'Школа' | 'Гимназия' | 'Лицей' | 'Колледж' | 'Техникум' | 'Другое'
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Students ON;
    INSERT INTO dbo.Students (StudentId,UserId,GroupId,DormitoryId,IsHeadman,StudentCode,BirthDate,BirthPlace,Gender,Citizenship,Address,SNILSNumber,PassportSeries,PassportNumber,PassportIssuedBy,PassportIssuedDate,PreviousSchool,PreviousSchoolType,StudyBasis,RoomNumber,EnrollmentDate,IsDeleted) VALUES
    (1, 4,1,1,   1,N'ЭБ-31-001','2004-03-12',N'г. Москва',  N'Мужской',N'Россия',N'г. Москва, ул. Ленина, д. 10, кв. 5',        N'001-002-003 04',N'4516',N'123456',N'ОВД Советского района г. Москвы',  '2020-03-12',N'ГБОУ СОШ №45 г. Москвы', N'Школа',N'Бюджет',   N'101','2022-09-01',0),
    (2, 6,1,1,   0,N'ЭБ-31-002','2004-07-25',N'г. Москва',  N'Женский', N'Россия',N'г. Москва, пр. Мира, д. 22, кв. 14',        N'002-003-004 05',N'4517',N'234567',N'ОВД Центрального района г. Москвы','2020-07-25',N'ГБОУ СОШ №112 г. Москвы',N'Школа',N'Бюджет',   N'102','2022-09-01',0),
    (3, 7,1,NULL,0,N'ЭБ-31-003','2004-11-08',N'г. Подольск',N'Мужской',N'Россия',N'г. Подольск, ул. Садовая, д. 3, кв. 7',      N'003-004-005 06',N'4518',N'345678',N'ОВД г. Подольска',                 '2020-11-08',N'МОУ СОШ №5 г. Подольска',N'Школа',N'Бюджет',   NULL,  '2022-09-01',0),
    (4, 8,1,2,   0,N'ЭБ-31-004','2005-02-14',N'г. Балашиха',N'Женский', N'Россия',N'г. Балашиха, ул. Первомайская, д. 18, кв. 3',N'004-005-006 07',N'4519',N'456789',N'ОВД г. Балашихи',                  '2021-02-14',N'МОУ СОШ №2 г. Балашихи', N'Школа',N'Контракт', N'215','2022-09-01',0),
    (5, 5,2,NULL,1,N'ИС-21-001','2005-05-20',N'г. Москва',  N'Мужской',N'Россия',N'г. Москва, ул. Гагарина, д. 7, кв. 11',      N'005-006-007 08',N'4520',N'567890',N'ОВД Южного района г. Москвы',      '2021-05-20',N'ГБОУ СОШ №78 г. Москвы', N'Школа',N'Контракт', NULL,  '2023-09-01',0),
    (6, 9,2,2,   0,N'ИС-21-002','2005-09-03',N'г. Химки',   N'Женский', N'Россия',N'г. Химки, ул. Молодёжная, д. 5, кв. 9',     N'006-007-008 09',N'4521',N'678901',N'ОВД г. Химки',                     '2021-09-03',N'МОУ СОШ №7 г. Химки',   N'Школа',N'Бюджет',   N'312','2023-09-01',0),
    (7,10,2,NULL,0,N'ИС-21-003','2005-12-17',N'г. Мытищи',  N'Мужской',N'Россия',N'г. Мытищи, пр. Олимпийский, д. 14, кв. 22', N'007-008-009 10',N'4522',N'789012',N'ОВД г. Мытищи',                    '2021-12-17',N'МОУ СОШ №3 г. Мытищи',  N'Школа',N'Бюджет',   NULL,  '2023-09-01',0),
    (8,11,2,1,   0,N'ИС-21-004','2005-04-29',N'г. Люберцы', N'Женский', N'Россия',N'г. Люберцы, ул. Октябрьская, д. 30, кв. 6', N'008-009-010 11',N'4523',N'890123',N'ОВД г. Люберцы',                   '2021-04-29',N'МОУ СОШ №11 г. Люберцы',N'Школа',N'Бюджет',   N'105','2023-09-01',0);
    SET IDENTITY_INSERT dbo.Students OFF;

    -- -------------------------------------------------------
    -- Subjects
    -- CK_Subjects_ControlType: 'Экзамен'|'Зачёт'|'Зачёт с оценкой'|'Дифференцированный зачёт'|'Курсовая работа'
    -- CK_Subjects_HoursSum: Total=0 OR Total=Lecture+Practice+Lab+Self
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Subjects ON;
    INSERT INTO dbo.Subjects (SubjectId,GroupId,TeacherId,SubjectName,HoursTotal,HoursLecture,HoursPractice,HoursLab,HoursSelfStudy,Semester,ControlType,IsDeleted) VALUES
    (1,1,1,N'Бухгалтерский учёт и отчётность',72,24,32, 0,16,N'5',N'Экзамен',0),
    (2,1,3,N'Налоги и налогообложение',        54,18,28, 0, 8,N'5',N'Зачёт',  0),
    (3,1,1,N'Финансы организаций',             60,20,28, 0,12,N'5',N'Экзамен',0),
    (4,1,5,N'Экономический анализ',            54,18,24, 0,12,N'5',N'Зачёт',  0),
    (5,2,2,N'Основы программирования',         90,30,20,30,10,N'3',N'Экзамен',0),
    (6,2,4,N'Базы данных',                     72,24,16,24, 8,N'3',N'Экзамен',0),
    (7,2,2,N'Веб-технологии',                  60,20,12,20, 8,N'3',N'Зачёт',  0),
    (8,2,4,N'Операционные системы',            54,18,12,18, 6,N'3',N'Зачёт',  0);
    SET IDENTITY_INSERT dbo.Subjects OFF;

    -- -------------------------------------------------------
    -- Schedule
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Schedule ON;
    INSERT INTO dbo.Schedule (ScheduleId,GroupId,SubjectId,DayOfWeek,LessonNumber,Classroom,WeekType,StartTime,EndTime,IsDeleted) VALUES
    (1, 1,1,1,1,N'ауд. 201',N'Обе','08:30','10:05',0),(2, 1,2,1,2,N'ауд. 203',N'Обе','10:15','11:50',0),
    (3, 1,3,2,1,N'ауд. 201',N'Обе','08:30','10:05',0),(4, 1,4,2,2,N'ауд. 115',N'Обе','10:15','11:50',0),
    (5, 1,1,3,3,N'ауд. 201',N'Обе','12:30','14:05',0),(6, 1,3,4,1,N'ауд. 203',N'Обе','08:30','10:05',0),
    (7, 2,5,1,3,N'ауд. 301',N'Обе','12:30','14:05',0),(8, 2,6,2,3,N'ауд. 307',N'Обе','12:30','14:05',0),
    (9, 2,5,3,1,N'ауд. 301',N'Обе','08:30','10:05',0),(10,2,7,3,2,N'ауд. 302',N'Обе','10:15','11:50',0),
    (11,2,8,4,2,N'ауд. 305',N'Обе','10:15','11:50',0),(12,2,6,5,4,N'ауд. 307',N'Обе','14:15','15:50',0);
    SET IDENTITY_INSERT dbo.Schedule OFF;

    -- -------------------------------------------------------
    -- StudentSocialInfo
    -- CK_Social_HealthGroup:    NULL | 'I'..'V'
    -- CK_Social_FamilyStructure: NULL | 'Полная' | 'Неполная (мать)' | 'Неполная (отец)' | 'Опекунство' | 'Детский дом' | 'Другое'
    -- CK_Social_DisabilityGroup: NULL | 'I' | 'II' | 'III'
    -- CK_Social_Housing:        NULL | 'Собственное жильё' | 'Аренда' | 'Общежитие' | 'С родственниками' | 'Другое'
    -- CK_Social_DisabilityLogic: DisabilityGroup IS NULL OR Disability IS NOT NULL
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.StudentSocialInfo ON;
    INSERT INTO dbo.StudentSocialInfo (SocialInfoId,StudentId,HealthGroup,ChronicDiseases,Disability,DisabilityGroup,DisabilityCertificate,IsOrphan,IsHalfOrphan,IsFromLargeFamily,IsLowIncome,IsSociallyVulnerable,IsOnGuardianship,SocialBenefits,PsychologicalFeatures,HousingCondition,FamilyStructure,FamilyType,AdditionalNotes) VALUES
    (1,1, N'I',  NULL,                      NULL,  NULL,  NULL,               0,0,0,0,0,0, NULL,                                        NULL,                                    N'Собственное жильё',N'Полная',           N'Благополучная',   NULL),
    (2,2, N'II', N'Аллергический ринит',    NULL,  NULL,  NULL,               0,1,0,1,0,0, N'Социальная стипендия',                     N'Повышенная тревожность',               N'Общежитие',        N'Неполная (мать)',   N'Малообеспеченная',N'Соц. поддержка по потере кормильца'),
    (3,3, N'I',  NULL,                      NULL,  NULL,  NULL,               0,0,1,0,0,0, N'Льгота многодетной семьи',                 NULL,                                    N'Собственное жильё',N'Полная',           N'Многодетная',     NULL),
    (4,5, N'III',N'Сахарный диабет II типа',N'Ограниченные возможности здоровья (ОВЗ)',N'III',N'МСЭ-2021-001234',0,0,0,0,1,0,N'Повышенная стипендия, бесплатное питание',N'Требует индивидуального сопровождения',N'Собственное жильё',N'Полная',N'Благополучная',N'Контроль уровня сахара'),
    (5,6, N'I',  NULL,                      NULL,  NULL,  NULL,               0,0,0,0,0,0, NULL,                                        NULL,                                    N'Общежитие',        N'Полная',           N'Благополучная',   NULL);
    SET IDENTITY_INSERT dbo.StudentSocialInfo OFF;

    -- -------------------------------------------------------
    -- Parents
    -- CK_Parents_Education: NULL | 'Высшее' | 'Неоконченное высшее' | 'Среднее профессиональное' | 'Среднее общее' | 'Основное общее' | 'Другое'
    -- CK_Parents_Relation:  'Мать' | 'Отец' | 'Опекун' | 'Попечитель' | 'Бабушка' | 'Дедушка' | 'Другой родственник'
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Parents ON;
    INSERT INTO dbo.Parents (ParentId,StudentId,Relation,LastName,FirstName,MiddleName,BirthDate,Phone,WorkPhone,Email,Address,Workplace,Position,Department,Education,IsMainContact,IsDeceased,HasParentalRights,IsDeleted) VALUES
    (1,1,N'Отец',N'Смирнов', N'Андрей',  N'Викторович',   '1975-06-15',N'+79001100001',N'+74951110001',N'smirnov.av@mail.ru', N'г. Москва, ул. Ленина, д. 10, кв. 5',         N'ООО «СтройГрупп»',            N'Инженер',               N'Производственный отдел', N'Высшее',                   1,0,1,0),
    (2,1,N'Мать',N'Смирнова',N'Ольга',   N'Николаевна',   '1978-09-22',N'+79001100002',N'+74951110002',N'smirnova.on@mail.ru',N'г. Москва, ул. Ленина, д. 10, кв. 5',         N'ГБУЗ «Городская больница №5»',N'Врач-терапевт',         N'Терапевтическое отделение',N'Высшее',                   0,0,1,0),
    (3,2,N'Мать',N'Козлова', N'Светлана',N'Ивановна',     '1980-03-08',N'+79001100003',N'+74951110003',N'kozlova.si@mail.ru', N'г. Москва, пр. Мира, д. 22, кв. 14',          N'МБОУ СОШ №112',               N'Учитель нач. классов',  N'Начальная школа',         N'Высшее',                   1,0,1,0),
    (4,3,N'Отец',N'Новиков', N'Владимир',N'Степанович',   '1972-11-30',N'+79001100004',N'+74951110004',N'novikov.vs@mail.ru', N'г. Подольск, ул. Садовая, д. 3, кв. 7',       N'МУП «Подольскводоканал»',     N'Слесарь-сантехник',     N'Эксплуатационный отдел', N'Среднее профессиональное', 1,0,1,0),
    (5,4,N'Мать',N'Лебедева',N'Татьяна', N'Александровна','1982-07-19',N'+79001100005',N'+74951110005',N'lebedeva.ta@mail.ru',N'г. Балашиха, ул. Первомайская, д. 18, кв. 3', N'ПАО «Балашихинский завод»',   N'Бухгалтер',             N'Финансовый отдел',        N'Высшее',                   1,0,1,0),
    (6,5,N'Отец',N'Морозов', N'Евгений', N'Павлович',     '1976-04-05',N'+79001100006',N'+74951110006',N'morozov.ep@mail.ru', N'г. Москва, ул. Гагарина, д. 7, кв. 11',       N'АО «ИнфоТех»',                N'Системный администратор',N'IT-отдел',               N'Высшее',                   1,0,1,0),
    (7,5,N'Мать',N'Морозова',N'Ирина',   N'Сергеевна',    '1979-12-10',N'+79001100007',N'+74951110007',N'morozova.is@mail.ru',N'г. Москва, ул. Гагарина, д. 7, кв. 11',       N'ГБОУ СОШ №78',                N'Преподаватель математики',N'Отдел точных наук',     N'Высшее',                   0,0,1,0);
    SET IDENTITY_INSERT dbo.Parents OFF;

    -- -------------------------------------------------------
    -- Grades
    -- CK_Grades_Type: 'Текущий'|'Рубежный'|'Экзамен'|'Зачёт'|'Курсовая работа'|'Практика'
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Grades ON;
    INSERT INTO dbo.Grades (GradeId,StudentId,SubjectId,AddedById,GradeValue,GradeDate,GradeType,Comment,IsDeleted) VALUES
    (1, 1,1,2,5,'2025-01-14',N'Текущий',NULL,0),(2, 1,2,2,4,'2025-01-28',N'Текущий',NULL,0),(3, 1,3,2,5,'2025-02-11',N'Текущий',NULL,0),
    (4, 2,1,2,4,'2025-01-14',N'Текущий',NULL,0),(5, 2,2,2,3,'2025-01-28',N'Текущий',N'Необходимо улучшить знания',0),(6, 2,4,2,4,'2025-02-18',N'Текущий',NULL,0),
    (7, 3,1,2,3,'2025-01-21',N'Текущий',N'Требуется доработка',0),(8, 3,3,2,4,'2025-02-04',N'Текущий',NULL,0),(9, 3,4,2,4,'2025-03-04',N'Текущий',NULL,0),
    (10,4,1,2,4,'2025-01-21',N'Текущий',NULL,0),(11,4,2,2,5,'2025-02-04',N'Текущий',N'Отличный результат',0),(12,4,3,2,4,'2025-03-11',N'Текущий',NULL,0),
    (13,5,5,3,5,'2025-01-15',N'Текущий',NULL,0),(14,5,6,3,5,'2025-02-05',N'Текущий',NULL,0),(15,5,7,3,4,'2025-03-05',N'Текущий',NULL,0),
    (16,6,5,3,4,'2025-01-15',N'Текущий',NULL,0),(17,6,6,3,3,'2025-02-05',N'Текущий',N'Нужно повторить тему JOIN',0),(18,6,8,3,4,'2025-03-19',N'Текущий',NULL,0),
    (19,7,5,3,3,'2025-01-22',N'Текущий',N'Сдан со второй попытки',0),(20,7,7,3,4,'2025-02-12',N'Текущий',NULL,0),(21,7,8,3,4,'2025-03-26',N'Текущий',NULL,0),
    (22,8,5,3,5,'2025-01-22',N'Текущий',NULL,0),(23,8,6,3,4,'2025-02-19',N'Текущий',NULL,0),(24,8,7,3,5,'2025-04-02',N'Текущий',N'Лучший результат в группе',0);
    SET IDENTITY_INSERT dbo.Grades OFF;

    -- -------------------------------------------------------
    -- Attendance
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Attendance ON;
    INSERT INTO dbo.Attendance (AttendanceId,StudentId,ScheduleId,MarkedById,LessonDate,Status,Reason,IsDeleted) VALUES
    (1, 1,1,2,'2025-04-07',N'Присутствовал',NULL,0),(2, 2,1,2,'2025-04-07',N'Присутствовал',NULL,0),
    (3, 3,1,2,'2025-04-07',N'Отсутствовал', N'Болезнь (справка)',0),(4, 4,1,2,'2025-04-07',N'Опоздал',NULL,0),
    (5, 1,3,2,'2025-04-08',N'Присутствовал',NULL,0),(6, 2,3,2,'2025-04-08',N'Присутствовал',NULL,0),
    (7, 3,3,2,'2025-04-08',N'Отсутствовал', N'Болезнь (справка)',0),(8, 4,3,2,'2025-04-08',N'Присутствовал',NULL,0),
    (9, 1,5,2,'2025-04-09',N'Присутствовал',NULL,0),(10,2,5,2,'2025-04-09',N'Опоздал',NULL,0),
    (11,5,7,3,'2025-04-07',N'Присутствовал',NULL,0),(12,6,7,3,'2025-04-07',N'Присутствовал',NULL,0),
    (13,7,7,3,'2025-04-07',N'Отсутствовал', N'Семейные обстоятельства',0),(14,8,7,3,'2025-04-07',N'Присутствовал',NULL,0),
    (15,5,8,3,'2025-04-08',N'Присутствовал',NULL,0),(16,6,8,3,'2025-04-08',N'Присутствовал',NULL,0),
    (17,7,8,3,'2025-04-08',N'Присутствовал',NULL,0),(18,8,8,3,'2025-04-08',N'Опоздал',NULL,0),
    (19,5,9,3,'2025-04-09',N'Присутствовал',NULL,0),(20,6,9,3,'2025-04-09',N'Отсутствовал',N'Участие в соревнованиях',0);
    SET IDENTITY_INSERT dbo.Attendance OFF;

    -- -------------------------------------------------------
    -- Achievements
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Achievements ON;
    INSERT INTO dbo.Achievements (AchievementId,StudentId,AddedById,Title,Category,Level,Description,AchieveDate,DocumentNumber,IsDeleted) VALUES
    (1,1,2,N'Победитель олимпиады по бухгалтерскому учёту',  N'Академическая',   N'Региональный',N'1 место в олимпиаде по бухгалтерскому учёту среди СПО',       '2025-02-20',N'ОЛ-2025-0045', 0),
    (2,1,2,N'Лучший студент семестра',                        N'Академическая',   N'Учреждение',  N'Лучший студент факультета по итогам 5 семестра',              '2025-01-31',N'ПР-2025-0012', 0),
    (3,4,2,N'Участник конференции «Молодой экономист»',       N'Научная',         N'Региональный',N'Доклад «Налоговое планирование в малом бизнесе»',             '2025-03-15',N'КОН-2025-0089',0),
    (4,5,3,N'Победитель хакатона по программированию',        N'Профессиональная',N'Региональный',N'1 место в командном хакатоне «IT-старт»',                     '2025-03-28',N'ХАК-2025-0017',0),
    (5,5,3,N'Стипендия губернатора',                          N'Академическая',   N'Региональный',N'Именная стипендия за высокие академические результаты',       '2025-02-01',N'СТИ-2025-0003',0),
    (6,8,3,N'Призёр конкурса веб-разработки',                 N'Профессиональная',N'Региональный',N'3 место на региональном конкурсе «Лучший сайт»',              '2025-04-05',N'КОН-2025-0102',0);
    SET IDENTITY_INSERT dbo.Achievements OFF;

    -- -------------------------------------------------------
    -- Documents
    -- -------------------------------------------------------
    SET IDENTITY_INSERT dbo.Documents ON;
    INSERT INTO dbo.Documents (DocumentId,GroupId,UploadedById,Title,DocumentType,FilePath,FileSize,UploadedAt,Description,IsDeleted) VALUES
    (1,1,2,N'Учебный план ЭБ-31 на 2024-2025',N'Учебный план',N'/documents/groups/1/plan_eb31.pdf', N'512 KB','2024-09-02 10:00:00',N'Утверждённый учебный план группы',0),
    (2,1,2,N'Приказ о зачислении ЭБ-31',      N'Приказ',      N'/documents/groups/1/order_eb31.pdf',N'128 KB','2024-09-02 10:15:00',N'Приказ о зачислении студентов ЭБ-31',0),
    (3,2,3,N'Учебный план ИС-21 на 2024-2025',N'Учебный план',N'/documents/groups/2/plan_is21.pdf', N'498 KB','2024-09-02 11:00:00',N'Утверждённый учебный план группы',0),
    (4,2,3,N'Приказ о зачислении ИС-21',      N'Приказ',      N'/documents/groups/2/order_is21.pdf',N'134 KB','2024-09-02 11:20:00',N'Приказ о зачислении студентов ИС-21',0);
    SET IDENTITY_INSERT dbo.Documents OFF;

    COMMIT TRANSACTION;

    PRINT N'';
    PRINT N'================================================';
    PRINT N'  Готово! Тестовые данные загружены.';
    PRINT N'  Пароль для всех учётных записей: test123';
    PRINT N'================================================';
    PRINT N'  admin       — Администратор';
    PRINT N'  ivanova_m   — Куратор ЭБ-31';
    PRINT N'  petrov_a    — Куратор ИС-21';
    PRINT N'  smirnov_d   — Cтароста ЭБ-31';
    PRINT N'  morozov_n   — Cтароста ИС-21';
    PRINT N'  kozlova_a / novikov_ar / lebedeva_o — Студенты ЭБ-31';
    PRINT N'  zaitseva_d / orlov_m / sokolova_v   — Студенты ИС-21';
    PRINT N'================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT N'ОШИБКА #' + CAST(ERROR_NUMBER() AS NVARCHAR) +
          N' стр.' + CAST(ERROR_LINE() AS NVARCHAR) +
          N': ' + ERROR_MESSAGE();
END CATCH;
GO
