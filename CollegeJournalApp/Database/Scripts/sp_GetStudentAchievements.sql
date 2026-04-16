USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_GetStudentAchievements', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentAchievements;
GO

CREATE PROCEDURE dbo.sp_GetStudentAchievements
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.Title,
        a.Category,
        ISNULL(a.Level,       N'') AS Level,
        ISNULL(a.Description, N'') AS Description,
        a.AchieveDate
    FROM Achievements a
    WHERE a.StudentId = @StudentId
      AND a.IsDeleted = 0
    ORDER BY a.AchieveDate DESC;
END;
GO
