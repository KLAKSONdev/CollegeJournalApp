USE CollegeJournal;
GO

-- Главная выборка посещаемости с учётом роли пользователя:
-- Admin   → все записи всех групп
-- Curator → записи своей группы
-- Headman → записи своей группы
-- Teacher → записи по дисциплинам, которые ведёт преподаватель
-- Student → только свои записи
CREATE OR ALTER PROCEDURE dbo.sp_GetAttendanceReport
    @UserId   INT,
    @RoleName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StudentId INT = NULL;
    DECLARE @GroupId   INT = NULL;
    DECLARE @TeacherId INT = NULL;

    IF @RoleName = 'Student'
        SELECT @StudentId = StudentId, @GroupId = GroupId
        FROM dbo.Students WHERE UserId = @UserId AND IsDeleted = 0;

    ELSE IF @RoleName = 'Headman'
        SELECT @GroupId = GroupId FROM dbo.Students WHERE UserId = @UserId AND IsDeleted = 0;

    ELSE IF @RoleName = 'Curator'
        SELECT TOP 1 @GroupId = GroupId FROM dbo.Groups
        WHERE CuratorId = @UserId AND IsDeleted = 0;

    ELSE IF @RoleName = 'Teacher'
        SELECT @TeacherId = TeacherId FROM dbo.Teachers
        WHERE UserId = @UserId AND IsDeleted = 0;

    SELECT
        a.AttendanceId,
        a.LessonDate,
        u.LastName + ' ' + u.FirstName +
            CASE WHEN u.MiddleName IS NOT NULL AND LTRIM(RTRIM(u.MiddleName)) != ''
                 THEN ' ' + LTRIM(RTRIM(u.MiddleName)) ELSE '' END  AS StudentName,
        g.GroupName,
        sub.SubjectName,
        a.Status,
        ISNULL(a.Reason, N'')                                       AS Reason,
        ISNULL(ub.LastName + ' ' + ub.FirstName, N'—')             AS MarkedByName,
        a.StudentId,
        a.ScheduleId
    FROM dbo.Attendance a
    INNER JOIN dbo.Students st  ON st.StudentId  = a.StudentId   AND st.IsDeleted  = 0
    INNER JOIN dbo.Users    u   ON u.UserId       = st.UserId     AND u.IsDeleted   = 0
    INNER JOIN dbo.Schedule sch ON sch.ScheduleId = a.ScheduleId  AND sch.IsDeleted = 0
    INNER JOIN dbo.Subjects sub ON sub.SubjectId  = sch.SubjectId AND sub.IsDeleted = 0
    INNER JOIN dbo.Groups   g   ON g.GroupId      = st.GroupId    AND g.IsDeleted   = 0
    LEFT  JOIN dbo.Users    ub  ON ub.UserId       = a.MarkedById
    WHERE a.IsDeleted = 0
      AND (
          @RoleName = 'Admin'
          OR (@RoleName = 'Student'  AND a.StudentId   = @StudentId)
          OR (@RoleName = 'Headman'  AND st.GroupId    = @GroupId)
          OR (@RoleName = 'Curator'  AND st.GroupId    = @GroupId)
          OR (@RoleName = 'Teacher'  AND sub.TeacherId = @TeacherId)
      )
    ORDER BY a.LessonDate DESC, g.GroupName, u.LastName;
END;
GO
