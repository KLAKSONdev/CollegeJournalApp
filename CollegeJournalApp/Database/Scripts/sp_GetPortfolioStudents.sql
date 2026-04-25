USE CollegeJournal;
GO

-- Список студентов для страницы портфолио.
-- @GroupId NULL = все группы (для администратора).
CREATE OR ALTER PROCEDURE dbo.sp_GetPortfolioStudents
    @GroupId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.StudentId,
        u.LastName + N' ' + u.FirstName
            + ISNULL(N' ' + u.MiddleName, N'')  AS FullName,
        u.LastName,
        u.FirstName,
        g.GroupName,
        g.GroupId,
        s.IsHeadman,
        s.StudentCode,
        (
            SELECT COUNT(*)
            FROM dbo.Achievements a
            WHERE a.StudentId = s.StudentId AND a.IsDeleted = 0
        )                                        AS AchievementCount
    FROM dbo.Students s
    JOIN dbo.Users  u ON u.UserId  = s.UserId  AND u.IsDeleted = 0
    JOIN dbo.Groups g ON g.GroupId = s.GroupId AND g.IsDeleted = 0
    WHERE s.IsDeleted = 0
      AND (@GroupId IS NULL OR s.GroupId = @GroupId)
    ORDER BY g.GroupName, u.LastName, u.FirstName;
END;
GO
