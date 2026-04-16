USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetTodaySchedule
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TodayDow TINYINT =
        CASE WHEN DATEPART(dw, GETDATE()) = 1 THEN 7
             ELSE CAST(DATEPART(dw, GETDATE()) - 1 AS TINYINT) END;

    SELECT
        sc.LessonNumber,
        sub.SubjectName,
        ISNULL(sc.Classroom, N'—')                                           AS Classroom,
        CONVERT(NVARCHAR(5), sc.StartTime, 108)                              AS StartTime,
        CONVERT(NVARCHAR(5), sc.EndTime,   108)                              AS EndTime,
        t.LastName + N' ' + LEFT(t.FirstName, 1) + N'.'
            + CASE WHEN t.MiddleName IS NOT NULL AND LEN(RTRIM(t.MiddleName)) > 0
                   THEN LEFT(t.MiddleName, 1) + N'.' ELSE N'' END            AS TeacherShort
    FROM dbo.Schedule sc
    INNER JOIN dbo.Subjects sub ON sub.SubjectId = sc.SubjectId
    INNER JOIN dbo.Teachers  t  ON t.TeacherId   = sub.TeacherId
    WHERE sc.GroupId   = @GroupId
      AND sc.DayOfWeek = @TodayDow
      AND sc.IsDeleted = 0
    ORDER BY sc.LessonNumber;
END;
GO
