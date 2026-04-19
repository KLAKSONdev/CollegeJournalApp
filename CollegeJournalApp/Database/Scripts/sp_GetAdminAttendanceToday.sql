USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAdminAttendanceToday
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.GroupId,
        g.GroupName,
        COUNT(DISTINCT st.StudentId)                                                  AS TotalStudents,
        ISNULL(SUM(CASE WHEN a.Status = N'Присутствовал' THEN 1 ELSE 0 END), 0)     AS PresentCount,
        ISNULL(SUM(CASE WHEN a.Status = N'Отсутствовал'  THEN 1 ELSE 0 END), 0)     AS AbsentCount,
        ISNULL(SUM(CASE WHEN a.Status = N'Опоздал'       THEN 1 ELSE 0 END), 0)     AS LateCount
    FROM dbo.Groups g
    INNER JOIN dbo.Students  st ON st.GroupId   = g.GroupId AND st.IsDeleted = 0
    LEFT  JOIN dbo.Attendance a  ON a.StudentId  = st.StudentId
                                AND a.IsDeleted  = 0
                                AND CAST(a.LessonDate AS DATE) = CAST(GETDATE() AS DATE)
    WHERE g.IsDeleted = 0 AND g.IsGraduated = 0
    GROUP BY g.GroupId, g.GroupName
    ORDER BY g.GroupName;
END;
GO
