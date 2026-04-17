USE CollegeJournal;
GO

-- Заменяем уникальный индекс на фильтрованный:
-- NULL-значения больше не конкурируют между собой (каждый студент без кода — ок)
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_Students_Code' AND object_id = OBJECT_ID('dbo.Students')
)
BEGIN
    ALTER TABLE dbo.Students DROP CONSTRAINT UQ_Students_Code;
END
GO

CREATE UNIQUE INDEX UX_Students_Code
    ON dbo.Students (StudentCode)
    WHERE StudentCode IS NOT NULL;
GO
