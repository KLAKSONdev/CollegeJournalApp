USE CollegeJournal;
GO

-- Таблица характеристик студентов (куратор пишет текст)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'StudentCharacteristic'
)
BEGIN
    CREATE TABLE dbo.StudentCharacteristic
    (
        CharacteristicId   INT           IDENTITY(1,1) PRIMARY KEY,
        StudentId          INT           NOT NULL UNIQUE
                               REFERENCES dbo.Students(StudentId),
        CharacteristicText NVARCHAR(MAX) NOT NULL DEFAULT N'',
        WrittenById        INT           NULL
                               REFERENCES dbo.Users(UserId),
        UpdatedAt          DATETIME      NULL
    );
    PRINT N'Таблица StudentCharacteristic создана.';
END
ELSE
    PRINT N'Таблица StudentCharacteristic уже существует.';
GO
