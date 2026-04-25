USE CollegeJournal;
GO

-- Достижения студента для портфолио (с AchievementId для редактирования).
CREATE OR ALTER PROCEDURE dbo.sp_GetPortfolioAchievements
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.AchievementId,
        a.Title,
        ISNULL(a.Category,       N'Другое') AS Category,
        ISNULL(a.Level,          N'')        AS Level,
        ISNULL(a.Description,    N'')        AS Description,
        a.AchieveDate,
        ISNULL(a.DocumentNumber, N'')        AS DocumentNumber,
        u.LastName + N' ' + u.FirstName      AS AddedByName
    FROM dbo.Achievements a
    LEFT JOIN dbo.Users u ON u.UserId = a.AddedById
    WHERE a.StudentId = @StudentId
      AND a.IsDeleted = 0
    ORDER BY a.AchieveDate DESC, a.AchievementId DESC;
END;
GO
