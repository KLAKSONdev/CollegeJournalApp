USE CollegeJournal;
GO

-- Сводка по посещаемости студента для портфолио.
CREATE OR ALTER PROCEDURE dbo.sp_GetPortfolioAttendance
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Status,
        COUNT(*) AS Cnt
    FROM dbo.Attendance
    WHERE StudentId = @StudentId
      AND IsDeleted = 0
    GROUP BY Status;
END;
GO
