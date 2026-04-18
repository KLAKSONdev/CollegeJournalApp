USE CollegeJournal;
GO

-- Возвращает список студентов группы для выбранного занятия.
-- Включает уже выставленные отметки (если были), иначе статус = Присутствовал.
CREATE OR ALTER PROCEDURE dbo.sp_GetStudentsForMarking
    @ScheduleId INT,
    @LessonDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @GroupId INT;
    SELECT @GroupId = GroupId FROM dbo.Schedule
    WHERE ScheduleId = @ScheduleId AND IsDeleted = 0;

    SELECT
        st.StudentId,
        u.LastName + ' ' + u.FirstName +
            CASE WHEN u.MiddleName IS NOT NULL AND LTRIM(RTRIM(u.MiddleName)) != ''
                 THEN ' ' + LTRIM(RTRIM(u.MiddleName)) ELSE '' END AS StudentName,
        ISNULL(a.Status, N'Присутствовал')  AS CurrentStatus,
        ISNULL(a.Reason, N'')               AS CurrentReason,
        a.AttendanceId
    FROM dbo.Students st
    INNER JOIN dbo.Users u ON u.UserId = st.UserId AND u.IsDeleted = 0
    LEFT JOIN dbo.Attendance a
           ON a.StudentId  = st.StudentId
          AND a.ScheduleId = @ScheduleId
          AND a.LessonDate = @LessonDate
          AND a.IsDeleted  = 0
    WHERE st.GroupId  = @GroupId
      AND st.IsDeleted = 0
    ORDER BY u.LastName, u.FirstName;
END;
GO
