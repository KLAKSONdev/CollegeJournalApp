USE CollegeJournal;
GO

-- Добавляем UserId в Teachers (если ещё нет)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Teachers') AND name = 'UserId'
)
BEGIN
    ALTER TABLE dbo.Teachers
        ADD UserId INT NULL
            REFERENCES dbo.Users(UserId);
    PRINT 'Колонка UserId добавлена в Teachers';
END
ELSE
    PRINT 'Колонка UserId уже существует';
GO

-- Добавляем роль Teacher (если ещё нет)
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Teacher')
BEGIN
    INSERT INTO dbo.Roles (RoleName, Description)
    VALUES (N'Teacher', N'Преподаватель — доступ к расписанию и оценкам своих групп');
    PRINT 'Роль Teacher добавлена';
END
ELSE
    PRINT 'Роль Teacher уже существует';
GO
