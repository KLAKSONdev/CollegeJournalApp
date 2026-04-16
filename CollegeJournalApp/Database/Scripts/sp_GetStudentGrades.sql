USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_GetStudentGrades', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentGrades;
GO

CREATE PROCEDURE dbo.sp_GetStudentGrades
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sub.SubjectName,
        g.GradeType,
        CAST(g.GradeValue AS NVARCHAR(5)) AS GradeValue,
        g.GradeDate
    FROM Grades g
    INNER JOIN Subjects sub ON sub.SubjectId = g.SubjectId
    WHERE g.StudentId = @StudentId
      AND g.IsDeleted = 0
    ORDER BY g.GradeDate DESC;
END;
GO
