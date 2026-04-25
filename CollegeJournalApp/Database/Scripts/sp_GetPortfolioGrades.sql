USE CollegeJournal;
GO

-- Сводка по успеваемости студента для портфолио.
CREATE OR ALTER PROCEDURE dbo.sp_GetPortfolioGrades
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Средний балл по каждому предмету
    SELECT
        sub.SubjectName,
        CAST(AVG(CAST(g.GradeValue AS FLOAT)) AS DECIMAL(4,2)) AS AvgGrade,
        COUNT(*)                                                AS GradeCount,
        MAX(g.GradeDate)                                        AS LastGradeDate
    FROM dbo.Grades   g
    JOIN dbo.Subjects sub ON sub.SubjectId = g.SubjectId
    WHERE g.StudentId = @StudentId
      AND g.IsDeleted = 0
    GROUP BY sub.SubjectId, sub.SubjectName
    ORDER BY AvgGrade DESC;
END;
GO
