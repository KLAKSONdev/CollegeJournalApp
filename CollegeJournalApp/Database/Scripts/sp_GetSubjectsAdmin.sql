USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetSubjectsAdmin
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        sub.SubjectId,
        sub.SubjectName,
        sub.GroupId,
        g.GroupName,
        sub.TeacherId,
        ISNULL(
            t.LastName + N' ' + t.FirstName +
            CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != ''
                 THEN N' ' + LTRIM(RTRIM(t.MiddleName)) ELSE N'' END,
            N'—') AS TeacherName,
        sub.HoursTotal, sub.HoursLecture, sub.HoursPractice,
        sub.HoursLab,   sub.HoursSelfStudy,
        sub.Semester,   sub.ControlType
    FROM dbo.Subjects sub
    INNER JOIN dbo.Groups   g ON g.GroupId   = sub.GroupId   AND g.IsDeleted = 0
    LEFT  JOIN dbo.Teachers t ON t.TeacherId = sub.TeacherId AND t.IsDeleted = 0
    WHERE sub.IsDeleted = 0
    ORDER BY g.GroupName, sub.SubjectName;
END;
GO
