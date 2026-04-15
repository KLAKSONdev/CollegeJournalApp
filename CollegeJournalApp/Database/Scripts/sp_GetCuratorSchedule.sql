-- Возвращает расписание куратора как преподавателя.
-- Ищет записи Schedule, где преподаватель совпадает с пользователем-куратором по фамилии и имени.

CREATE OR ALTER PROCEDURE [dbo].[sp_GetCuratorSchedule]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Получаем фамилию и имя куратора из таблицы Users
    DECLARE @LastName  NVARCHAR(100);
    DECLARE @FirstName NVARCHAR(100);

    SELECT @LastName = LastName, @FirstName = FirstName
    FROM Users
    WHERE UserId = @UserId AND IsDeleted = 0;

    -- Расписание всех занятий, где куратор является преподавателем
    SELECT
        s.DayOfWeek,
        s.LessonNumber,
        ISNULL(sub.SubjectName, '—')  AS SubjectName,
        ISNULL(s.Classroom,     '—')  AS Classroom,
        t.LastName + ' ' + t.FirstName +
            CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != ''
                 THEN ' ' + LTRIM(RTRIM(t.MiddleName))
                 ELSE ''
            END                        AS TeacherName,
        ISNULL(g.GroupName,     '—')  AS GroupName
    FROM Schedule s
    INNER JOIN Subjects sub ON sub.SubjectId = s.SubjectId
    INNER JOIN Teachers t   ON t.TeacherId  = sub.TeacherId
    INNER JOIN Groups   g   ON g.GroupId    = s.GroupId
    WHERE t.LastName  = @LastName
      AND t.FirstName = @FirstName
      AND s.IsDeleted = 0
    ORDER BY s.DayOfWeek, s.LessonNumber;
END
