USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAdminAttendanceToday
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.GroupId,
        g.GroupName,
        COUNT(DISTINCT st.StudentId)                                                                        AS TotalStudents,
        COUNT(DISTINCT CASE WHEN a.Status = N'Присутствовал' THEN st.StudentId END)                       AS PresentCount,
        COUNT(DISTINCT CASE WHEN a.Status = N'Отсутствовал'  THEN st.StudentId END)                       AS AbsentCount,
        COUNT(DISTINCT CASE WHEN a.Status = N'Опоздал'       THEN st.StudentId END)                       AS LateCount
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
