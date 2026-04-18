USE CollegeJournal;
GO

-- Возвращает занятия на указанную дату для выставления посещаемости.
-- Teacher → только свои дисциплины
-- Headman → занятия своей группы
-- Admin   → все занятия дня
CREATE OR ALTER PROCEDURE dbo.sp_GetLessonsForMarking
    @UserId   INT,
    @RoleName NVARCHAR(50),
    @Date     DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- DayOfWeek: 1=Пн, 2=Вт, ..., 7=Вс  (DATEPART(dw) 1=Sun → 7, 2=Mon → 1 ...)
    DECLARE @DOW TINYINT =
        CASE WHEN DATEPART(dw, @Date) = 1 THEN 7
             ELSE CAST(DATEPART(dw, @Date) - 1 AS TINYINT) END;

    DECLARE @TeacherId INT = NULL;
    DECLARE @GroupId   INT = NULL;

    IF @RoleName = 'Teacher'
        SELECT @TeacherId = TeacherId FROM dbo.Teachers
        WHERE UserId = @UserId AND IsDeleted = 0;

    ELSE IF @RoleName = 'Headman'
        SELECT @GroupId = GroupId FROM dbo.Students
        WHERE UserId = @UserId AND IsDeleted = 0;

    SELECT
        sch.ScheduleId,
        sub.SubjectName,
        g.GroupName,
        g.GroupId,
        sch.LessonNumber,
        CONVERT(NVARCHAR(5), sch.StartTime, 108) AS StartTime,
        ISNULL(sch.Classroom, N'—')              AS Classroom
    FROM dbo.Schedule sch
    INNER JOIN dbo.Subjects sub ON sub.SubjectId = sch.SubjectId AND sub.IsDeleted = 0
    INNER JOIN dbo.Groups   g   ON g.GroupId     = sch.GroupId   AND g.IsDeleted  = 0
    WHERE sch.IsDeleted = 0
      AND sch.DayOfWeek = @DOW
      AND (
          @RoleName = 'Admin'
          OR (@RoleName = 'Teacher' AND sub.TeacherId = @TeacherId)
          OR (@RoleName = 'Headman' AND sch.GroupId   = @GroupId)
      )
    ORDER BY g.GroupName, sch.LessonNumber;
END;
GO
