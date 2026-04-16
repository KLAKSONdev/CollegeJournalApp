USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_GetStudentAttendance', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentAttendance;
GO

CREATE PROCEDURE dbo.sp_GetStudentAttendance
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.LessonDate,
        sub.SubjectName,
        a.Status,
        ISNULL(a.Reason, N'') AS Reason
    FROM Attendance a
    INNER JOIN Schedule sch ON sch.ScheduleId = a.ScheduleId
    INNER JOIN Subjects sub ON sub.SubjectId = sch.SubjectId
    WHERE a.StudentId = @StudentId
      AND a.IsDeleted = 0
    ORDER BY a.LessonDate DESC;
END;
GO
