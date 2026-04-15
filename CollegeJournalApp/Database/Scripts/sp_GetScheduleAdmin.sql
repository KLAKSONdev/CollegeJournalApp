-- Возвращает расписание для администратора с фильтрами по группе и/или преподавателю.
-- Включает ScheduleId, GroupId, SubjectId для возможности редактирования.

CREATE OR ALTER PROCEDURE [dbo].[sp_GetScheduleAdmin]
    @GroupId   INT = NULL,
    @TeacherId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @GroupId IS NULL AND @TeacherId IS NULL
    BEGIN
        SELECT TOP 0
            CAST(NULL AS INT)            AS ScheduleId,
            CAST(NULL AS TINYINT)        AS DayOfWeek,
            CAST(NULL AS TINYINT)        AS LessonNumber,
            CAST(NULL AS NVARCHAR(255))  AS SubjectName,
            CAST(NULL AS NVARCHAR(100))  AS Classroom,
            CAST(NULL AS NVARCHAR(300))  AS TeacherName,
            CAST(NULL AS NVARCHAR(100))  AS GroupName,
            CAST(NULL AS INT)            AS GroupId,
            CAST(NULL AS INT)            AS SubjectId;
        RETURN;
    END

    SELECT
        s.ScheduleId,
        s.DayOfWeek,
        s.LessonNumber,
        ISNULL(sub.SubjectName, '—') AS SubjectName,
        ISNULL(s.Classroom,     '—') AS Classroom,
        ISNULL(
            t.LastName + ' ' + t.FirstName +
            CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != ''
                 THEN ' ' + LTRIM(RTRIM(t.MiddleName)) ELSE '' END,
            '—'
        )                            AS TeacherName,
        ISNULL(g.GroupName,     '—') AS GroupName,
        s.GroupId,
        s.SubjectId
    FROM Schedule s
    INNER JOIN Groups   g   ON g.GroupId    = s.GroupId
    INNER JOIN Subjects sub ON sub.SubjectId = s.SubjectId
    LEFT  JOIN Teachers t   ON t.TeacherId  = sub.TeacherId
    WHERE s.IsDeleted = 0
      AND (@GroupId   IS NULL OR s.GroupId    = @GroupId)
      AND (@TeacherId IS NULL OR sub.TeacherId = @TeacherId)
    ORDER BY s.DayOfWeek, s.LessonNumber;
END
