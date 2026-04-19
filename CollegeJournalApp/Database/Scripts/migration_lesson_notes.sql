USE CollegeJournal;
GO

-- Таблица тем/типов уроков для конкретной даты
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'LessonNotes' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE dbo.LessonNotes
    (
        NoteId          INT IDENTITY(1,1) PRIMARY KEY,
        GroupId         INT  NOT NULL REFERENCES dbo.Groups(GroupId),
        SubjectId       INT  NOT NULL REFERENCES dbo.Subjects(SubjectId),
        LessonDate      DATE NOT NULL,
        NoteText        NVARCHAR(300) NOT NULL DEFAULT '',
        CreatedByUserId INT  NULL REFERENCES dbo.Users(UserId),
        CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt       DATETIME NULL,

        CONSTRAINT UQ_LessonNote UNIQUE (GroupId, SubjectId, LessonDate)
    );

    PRINT 'Таблица LessonNotes создана.';
END
ELSE
    PRINT 'Таблица LessonNotes уже существует — пропуск.';
GO
