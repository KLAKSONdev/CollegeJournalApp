USE CollegeJournal;
GO

-- Расписание конкретного преподавателя по его UserId
CREATE OR ALTER PROCEDURE dbo.sp_GetTeacherSchedule
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Получаем TeacherId по UserId
    DECLARE @TeacherId INT;
    SELECT @TeacherId = TeacherId FROM dbo.Teachers WHERE UserId = @UserId AND IsDeleted = 0;

    IF @TeacherId IS NULL
    BEGIN
        SELECT TOP 0
            CAST(NULL AS TINYINT)       AS DayOfWeek,
            CAST(NULL AS TINYINT)       AS LessonNumber,
            CAST(NULL AS NVARCHAR(255)) AS SubjectName,
            CAST(NULL AS NVARCHAR(100)) AS Classroom,
            CAST(NULL AS NVARCHAR(100)) AS GroupName,
            CAST(NULL AS TIME)          AS StartTime,
            CAST(NULL AS TIME)          AS EndTime;
        RETURN;
    END

    SELECT
        s.DayOfWeek,
        s.LessonNumber,
        ISNULL(sub.SubjectName, N'—')                         AS SubjectName,
        ISNULL(s.Classroom,     N'—')                         AS Classroom,
        ISNULL(g.GroupName,     N'—')                         AS GroupName,
        t.LastName + N' ' + LEFT(t.FirstName, 1) + N'.'
            + CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != N''
                   THEN LEFT(LTRIM(RTRIM(t.MiddleName)), 1) + N'.'
                   ELSE N'' END                               AS TeacherName,
        s.StartTime,
        s.EndTime
    FROM dbo.Schedule s
    INNER JOIN dbo.Subjects sub ON sub.SubjectId = s.SubjectId
    INNER JOIN dbo.Groups   g   ON g.GroupId     = s.GroupId
    INNER JOIN dbo.Teachers t   ON t.TeacherId   = @TeacherId
    WHERE sub.TeacherId = @TeacherId
      AND s.IsDeleted   = 0
    ORDER BY s.DayOfWeek, s.LessonNumber;
END;
GO
