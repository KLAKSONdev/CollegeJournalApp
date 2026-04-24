-- ============================================================
-- create_database.sql
-- Полное создание БД CollegeJournal с нуля
-- Выполнять от sa или dbcreator
-- ============================================================

USE master;
GO

IF DB_ID('CollegeJournal') IS NOT NULL
BEGIN
    ALTER DATABASE CollegeJournal SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CollegeJournal;
END
GO

CREATE DATABASE CollegeJournal
    COLLATE Cyrillic_General_CI_AS;
GO

USE CollegeJournal;
GO

-- ============================================================
-- AcademicYears
-- ============================================================
CREATE TABLE dbo.AcademicYears
(
    YearId      INT IDENTITY(1,1) PRIMARY KEY,
    Title       NVARCHAR(9)  NOT NULL,
    StartDate   DATE         NOT NULL,
    EndDate     DATE         NOT NULL,
    IsCurrent   BIT          NOT NULL DEFAULT 0,
    IsDeleted   BIT          NOT NULL DEFAULT 0,

    CONSTRAINT CK_AcademicYears_Title
        CHECK (Title LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]')
);
GO

-- ============================================================
-- Roles
-- ============================================================
CREATE TABLE dbo.Roles
(
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(20)  NOT NULL UNIQUE,
    Description NVARCHAR(200) NULL
);
GO

-- ============================================================
-- Users
-- ============================================================
CREATE TABLE dbo.Users
(
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    RoleId       INT           NOT NULL REFERENCES dbo.Roles(RoleId),
    Login        NVARCHAR(50)  NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    LastName     NVARCHAR(50)  NOT NULL,
    FirstName    NVARCHAR(50)  NOT NULL,
    MiddleName   NVARCHAR(50)  NULL,
    Phone        NVARCHAR(20)  NULL,
    Email        NVARCHAR(100) NULL,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME      NOT NULL DEFAULT GETDATE(),
    IsDeleted    BIT           NOT NULL DEFAULT 0,

    CONSTRAINT UQ_Users_Login UNIQUE (Login)
);
GO

-- ============================================================
-- Dormitories
-- ============================================================
CREATE TABLE dbo.Dormitories
(
    DormitoryId    INT IDENTITY(1,1) PRIMARY KEY,
    Name           NVARCHAR(100) NOT NULL,
    Address        NVARCHAR(200) NULL,
    CommandantName NVARCHAR(100) NULL,
    Phone          NVARCHAR(20)  NULL,
    TotalRooms     INT           NULL,
    IsDeleted      BIT           NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- Groups
-- ============================================================
CREATE TABLE dbo.Groups
(
    GroupId        INT IDENTITY(1,1) PRIMARY KEY,
    YearId         INT           NOT NULL REFERENCES dbo.AcademicYears(YearId),
    CuratorId      INT           NULL REFERENCES dbo.Users(UserId),
    GroupName      NVARCHAR(20)  NOT NULL,
    Specialty      NVARCHAR(200) NULL,
    SpecialtyCode  NVARCHAR(20)  NULL,
    Course         INT           NULL,
    Semester       INT           NULL,
    EducationForm  NVARCHAR(30)  NULL,
    EducationBasis NVARCHAR(20)  NULL,
    StudentCount   INT           NOT NULL DEFAULT 0,
    IsGraduated    BIT           NOT NULL DEFAULT 0,
    GraduationDate DATE          NULL,
    IsDeleted      BIT           NOT NULL DEFAULT 0,

    CONSTRAINT CK_Groups_EduBasis
        CHECK (EducationBasis IN (N'Бюджет', N'Контракт', N'Смешанная') OR EducationBasis IS NULL)
);
GO

-- ============================================================
-- Teachers
-- ============================================================
CREATE TABLE dbo.Teachers
(
    TeacherId  INT IDENTITY(1,1) PRIMARY KEY,
    LastName   NVARCHAR(50) NOT NULL,
    FirstName  NVARCHAR(50) NOT NULL,
    MiddleName NVARCHAR(50) NULL,
    IsActive   BIT          NOT NULL DEFAULT 1,
    IsDeleted  BIT          NOT NULL DEFAULT 0,
    UserId     INT          NULL REFERENCES dbo.Users(UserId)
);
GO

-- ============================================================
-- Students
-- ============================================================
CREATE TABLE dbo.Students
(
    StudentId          INT IDENTITY(1,1) PRIMARY KEY,
    UserId             INT           NOT NULL REFERENCES dbo.Users(UserId),
    GroupId            INT           NOT NULL REFERENCES dbo.Groups(GroupId),
    DormitoryId        INT           NULL REFERENCES dbo.Dormitories(DormitoryId),
    IsHeadman          BIT           NOT NULL DEFAULT 0,
    StudentCode        NVARCHAR(30)  NULL,
    BirthDate          DATE          NULL,
    BirthPlace         NVARCHAR(100) NULL,
    Gender             NVARCHAR(10)  NULL,
    Citizenship        NVARCHAR(50)  NULL,
    Address            NVARCHAR(300) NULL,
    SNILSNumber        NVARCHAR(30)  NULL,
    PassportSeries     NVARCHAR(4)   NULL,
    PassportNumber     NVARCHAR(6)   NULL,
    PassportIssuedBy   NVARCHAR(200) NULL,
    PassportIssuedDate DATE          NULL,
    PreviousSchool     NVARCHAR(200) NULL,
    PreviousSchoolType NVARCHAR(20)  NULL,
    StudyBasis         NVARCHAR(20)  NULL,
    RoomNumber         NVARCHAR(10)  NULL,
    EnrollmentDate     DATE          NULL,
    PhotoData          VARBINARY(MAX) NULL,
    PhotoMimeType      NVARCHAR(50)  NULL,
    IsDeleted          BIT           NOT NULL DEFAULT 0,

    CONSTRAINT CK_Students_StudyBasis
        CHECK (StudyBasis IN (N'Бюджет', N'Контракт') OR StudyBasis IS NULL),
    CONSTRAINT CK_Students_PrevSchoolType
        CHECK (PreviousSchoolType IN (N'Школа', N'Гимназия', N'Лицей', N'Колледж', N'Техникум', N'Другое') OR PreviousSchoolType IS NULL)
);
GO

CREATE UNIQUE INDEX UX_Students_Code
    ON dbo.Students (StudentCode)
    WHERE StudentCode IS NOT NULL;
GO

-- ============================================================
-- Subjects
-- ============================================================
CREATE TABLE dbo.Subjects
(
    SubjectId      INT IDENTITY(1,1) PRIMARY KEY,
    GroupId        INT           NOT NULL REFERENCES dbo.Groups(GroupId),
    TeacherId      INT           NULL REFERENCES dbo.Teachers(TeacherId),
    SubjectName    NVARCHAR(200) NOT NULL,
    HoursTotal     INT           NOT NULL DEFAULT 0,
    HoursLecture   INT           NOT NULL DEFAULT 0,
    HoursPractice  INT           NOT NULL DEFAULT 0,
    HoursLab       INT           NOT NULL DEFAULT 0,
    HoursSelfStudy INT           NOT NULL DEFAULT 0,
    Semester       NVARCHAR(5)   NULL,
    ControlType    NVARCHAR(50)  NULL,
    IsDeleted      BIT           NOT NULL DEFAULT 0,

    CONSTRAINT CK_Subjects_ControlType
        CHECK (ControlType IN (N'Экзамен', N'Зачёт', N'Зачёт с оценкой',
               N'Дифференцированный зачёт', N'Курсовая работа') OR ControlType IS NULL),
    CONSTRAINT CK_Subjects_HoursSum
        CHECK (HoursTotal = 0 OR HoursTotal = HoursLecture + HoursPractice + HoursLab + HoursSelfStudy)
);
GO

-- ============================================================
-- Schedule
-- ============================================================
CREATE TABLE dbo.Schedule
(
    ScheduleId   INT IDENTITY(1,1) PRIMARY KEY,
    GroupId      INT          NOT NULL REFERENCES dbo.Groups(GroupId),
    SubjectId    INT          NOT NULL REFERENCES dbo.Subjects(SubjectId),
    DayOfWeek    INT          NOT NULL,
    LessonNumber INT          NOT NULL,
    Classroom    NVARCHAR(50) NULL,
    WeekType     NVARCHAR(10) NOT NULL DEFAULT N'Обе',
    StartTime    TIME         NULL,
    EndTime      TIME         NULL,
    IsDeleted    BIT          NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- StudentSocialInfo
-- ============================================================
CREATE TABLE dbo.StudentSocialInfo
(
    SocialInfoId           INT IDENTITY(1,1) PRIMARY KEY,
    StudentId              INT            NOT NULL UNIQUE REFERENCES dbo.Students(StudentId),
    HealthGroup            NVARCHAR(2)    NULL,
    ChronicDiseases        NVARCHAR(300)  NULL,
    Disability             NVARCHAR(200)  NULL,
    DisabilityGroup        NVARCHAR(3)    NULL,
    DisabilityCertificate  NVARCHAR(50)   NULL,
    IsOrphan               BIT            NOT NULL DEFAULT 0,
    IsHalfOrphan           BIT            NOT NULL DEFAULT 0,
    IsFromLargeFamily      BIT            NOT NULL DEFAULT 0,
    IsLowIncome            BIT            NOT NULL DEFAULT 0,
    IsSociallyVulnerable   BIT            NOT NULL DEFAULT 0,
    IsOnGuardianship       BIT            NOT NULL DEFAULT 0,
    SocialBenefits         NVARCHAR(300)  NULL,
    PsychologicalFeatures  NVARCHAR(300)  NULL,
    HousingCondition       NVARCHAR(50)   NULL,
    FamilyStructure        NVARCHAR(30)   NULL,
    FamilyType             NVARCHAR(50)   NULL,
    AdditionalNotes        NVARCHAR(500)  NULL,

    CONSTRAINT CK_Social_HealthGroup
        CHECK (HealthGroup IN (N'I', N'II', N'III', N'IV', N'V') OR HealthGroup IS NULL),
    CONSTRAINT CK_Social_FamilyStructure
        CHECK (FamilyStructure IN (N'Полная', N'Неполная (мать)', N'Неполная (отец)',
               N'Опекунство', N'Детский дом', N'Другое') OR FamilyStructure IS NULL),
    CONSTRAINT CK_Social_DisabilityGroup
        CHECK (DisabilityGroup IN (N'I', N'II', N'III') OR DisabilityGroup IS NULL),
    CONSTRAINT CK_Social_Housing
        CHECK (HousingCondition IN (N'Собственное жильё', N'Аренда', N'Общежитие',
               N'С родственниками', N'Другое') OR HousingCondition IS NULL),
    CONSTRAINT CK_Social_DisabilityLogic
        CHECK (DisabilityGroup IS NULL OR Disability IS NOT NULL)
);
GO

-- ============================================================
-- Parents
-- ============================================================
CREATE TABLE dbo.Parents
(
    ParentId         INT IDENTITY(1,1) PRIMARY KEY,
    StudentId        INT           NOT NULL REFERENCES dbo.Students(StudentId),
    Relation         NVARCHAR(30)  NOT NULL,
    LastName         NVARCHAR(50)  NOT NULL,
    FirstName        NVARCHAR(50)  NOT NULL,
    MiddleName       NVARCHAR(50)  NULL,
    BirthDate        DATE          NULL,
    Phone            NVARCHAR(20)  NULL,
    WorkPhone        NVARCHAR(20)  NULL,
    Email            NVARCHAR(100) NULL,
    Address          NVARCHAR(300) NULL,
    Workplace        NVARCHAR(200) NULL,
    Position         NVARCHAR(100) NULL,
    Department       NVARCHAR(100) NULL,
    Education        NVARCHAR(50)  NULL,
    IsMainContact    BIT           NOT NULL DEFAULT 0,
    IsDeceased       BIT           NOT NULL DEFAULT 0,
    HasParentalRights BIT          NOT NULL DEFAULT 1,
    IsDeleted        BIT           NOT NULL DEFAULT 0,

    CONSTRAINT CK_Parents_Relation
        CHECK (Relation IN (N'Мать', N'Отец', N'Опекун', N'Попечитель',
               N'Бабушка', N'Дедушка', N'Другой родственник')),
    CONSTRAINT CK_Parents_Education
        CHECK (Education IN (N'Высшее', N'Неоконченное высшее', N'Среднее профессиональное',
               N'Среднее общее', N'Основное общее', N'Другое') OR Education IS NULL)
);
GO

-- ============================================================
-- Grades
-- ============================================================
CREATE TABLE dbo.Grades
(
    GradeId    INT IDENTITY(1,1) PRIMARY KEY,
    StudentId  INT          NOT NULL REFERENCES dbo.Students(StudentId),
    SubjectId  INT          NOT NULL REFERENCES dbo.Subjects(SubjectId),
    AddedById  INT          NOT NULL REFERENCES dbo.Users(UserId),
    GradeValue INT          NOT NULL,
    GradeDate  DATE         NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    GradeType  NVARCHAR(30) NOT NULL,
    Comment    NVARCHAR(300) NULL,
    IsDeleted  BIT          NOT NULL DEFAULT 0,

    CONSTRAINT CK_Grades_Value CHECK (GradeValue BETWEEN 1 AND 5),
    CONSTRAINT CK_Grades_Type
        CHECK (GradeType IN (N'Текущий', N'Рубежный', N'Экзамен',
               N'Зачёт', N'Курсовая работа', N'Практика'))
);
GO

-- ============================================================
-- Attendance
-- ============================================================
CREATE TABLE dbo.Attendance
(
    AttendanceId INT IDENTITY(1,1) PRIMARY KEY,
    StudentId    INT          NOT NULL REFERENCES dbo.Students(StudentId),
    ScheduleId   INT          NOT NULL REFERENCES dbo.Schedule(ScheduleId),
    MarkedById   INT          NOT NULL REFERENCES dbo.Users(UserId),
    LessonDate   DATE         NOT NULL,
    Status       NVARCHAR(20) NOT NULL,
    Reason       NVARCHAR(200) NULL,
    IsDeleted    BIT          NOT NULL DEFAULT 0,

    CONSTRAINT CK_Attendance_Status
        CHECK (Status IN (N'Присутствовал', N'Отсутствовал', N'Опоздал')),
    CONSTRAINT UQ_Attendance UNIQUE (StudentId, ScheduleId, LessonDate)
);
GO

-- ============================================================
-- Achievements
-- ============================================================
CREATE TABLE dbo.Achievements
(
    AchievementId  INT IDENTITY(1,1) PRIMARY KEY,
    StudentId      INT           NOT NULL REFERENCES dbo.Students(StudentId),
    AddedById      INT           NOT NULL REFERENCES dbo.Users(UserId),
    Title          NVARCHAR(200) NOT NULL,
    Category       NVARCHAR(50)  NULL,
    Level          NVARCHAR(50)  NULL,
    Description    NVARCHAR(500) NULL,
    AchieveDate    DATE          NULL,
    DocumentNumber NVARCHAR(50)  NULL,
    IsDeleted      BIT           NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- Documents
-- ============================================================
CREATE TABLE dbo.Documents
(
    DocumentId   INT IDENTITY(1,1) PRIMARY KEY,
    GroupId      INT           NOT NULL REFERENCES dbo.Groups(GroupId),
    UploadedById INT           NOT NULL REFERENCES dbo.Users(UserId),
    Title        NVARCHAR(200) NOT NULL,
    DocumentType NVARCHAR(50)  NULL,
    FilePath     NVARCHAR(500) NULL,
    FileSize     NVARCHAR(20)  NULL,
    UploadedAt   DATETIME      NOT NULL DEFAULT GETDATE(),
    Description  NVARCHAR(300) NULL,
    IsDeleted    BIT           NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- LessonNotes
-- ============================================================
CREATE TABLE dbo.LessonNotes
(
    NoteId          INT IDENTITY(1,1) PRIMARY KEY,
    GroupId         INT           NOT NULL REFERENCES dbo.Groups(GroupId),
    SubjectId       INT           NOT NULL REFERENCES dbo.Subjects(SubjectId),
    LessonDate      DATE          NOT NULL,
    NoteText        NVARCHAR(300) NOT NULL DEFAULT '',
    CreatedByUserId INT           NULL REFERENCES dbo.Users(UserId),
    CreatedAt       DATETIME      NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME      NULL,

    CONSTRAINT UQ_LessonNote UNIQUE (GroupId, SubjectId, LessonDate)
);
GO

-- ============================================================
-- AuditLog
-- ============================================================
CREATE TABLE dbo.AuditLog
(
    LogId     INT IDENTITY(1,1) PRIMARY KEY,
    UserId    INT           NOT NULL REFERENCES dbo.Users(UserId),
    Action    NVARCHAR(20)  NOT NULL,
    TableName NVARCHAR(50)  NOT NULL,
    RecordId  INT           NOT NULL,
    CreatedAt DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

PRINT N'';
PRINT N'================================================';
PRINT N'  База данных CollegeJournal создана успешно!';
PRINT N'  Теперь выполните fresh_seed.sql для тестовых';
PRINT N'  данных и все скрипты sp_*.sql для процедур.';
PRINT N'================================================';
