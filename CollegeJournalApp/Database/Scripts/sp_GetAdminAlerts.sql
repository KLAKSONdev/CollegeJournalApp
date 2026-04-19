USE CollegeJournal;
GO

-- Студенты, требующие внимания: >= 2 двоек за текущий месяц  ИЛИ >= 4 пропусков за 30 дней
CREATE OR ALTER PROCEDURE dbo.sp_GetAdminAlerts
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 20
        u.LastName + N' ' + LEFT(u.FirstName, 1) + N'.'    AS StudentName,
        g.GroupName,
        ISNULL(SUM(CASE
            WHEN gr.GradeValue = 2
             AND MONTH(gr.GradeDate) = MONTH(GETDATE())
             AND YEAR(gr.GradeDate)  = YEAR(GETDATE()) THEN 1 ELSE 0 END), 0)  AS TwosCount,
        ISNULL(SUM(CASE
            WHEN a.Status = N'Отсутствовал'
             AND a.LessonDate >= DATEADD(DAY, -30, GETDATE()) THEN 1 ELSE 0 END), 0) AS AbsencesCount
    FROM dbo.Students st
    INNER JOIN dbo.Users    u  ON u.UserId    = st.UserId    AND u.IsDeleted = 0
    INNER JOIN dbo.Groups   g  ON g.GroupId   = st.GroupId   AND g.IsDeleted = 0
    LEFT  JOIN dbo.Grades   gr ON gr.StudentId = st.StudentId AND gr.IsDeleted = 0
    LEFT  JOIN dbo.Attendance a ON a.StudentId = st.StudentId AND a.IsDeleted = 0
    WHERE st.IsDeleted = 0 AND g.IsGraduated = 0
    GROUP BY st.StudentId, u.LastName, u.FirstName, g.GroupName
    HAVING
        ISNULL(SUM(CASE
            WHEN gr.GradeValue = 2
             AND MONTH(gr.GradeDate) = MONTH(GETDATE())
             AND YEAR(gr.GradeDate)  = YEAR(GETDATE()) THEN 1 ELSE 0 END), 0) >= 2
        OR
        ISNULL(SUM(CASE
            WHEN a.Status = N'Отсутствовал'
             AND a.LessonDate >= DATEADD(DAY, -30, GETDATE()) THEN 1 ELSE 0 END), 0) >= 4
    ORDER BY TwosCount DESC, AbsencesCount DESC;
END;
GO
