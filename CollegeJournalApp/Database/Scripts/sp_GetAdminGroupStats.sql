USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAdminGroupStats
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.GroupId,
        g.GroupName,
        ISNULL(g.Course, 0)                                                          AS Course,
        COUNT(DISTINCT st.StudentId)                                                 AS StudentCount,
        CAST(ISNULL(AVG(CAST(gr.GradeValue AS DECIMAL(4,2))), 0) AS DECIMAL(4,2))  AS AvgGrade,
        -- Двойки за текущий месяц
        ISNULL(SUM(CASE
            WHEN gr.GradeValue = 2
             AND MONTH(gr.GradeDate) = MONTH(GETDATE())
             AND YEAR(gr.GradeDate)  = YEAR(GETDATE()) THEN 1 ELSE 0 END), 0)       AS FailCount,
        -- % отличников (5) от всех оценок
        CASE WHEN COUNT(gr.GradeId) > 0
             THEN CAST(SUM(CASE WHEN gr.GradeValue = 5 THEN 1 ELSE 0 END)
                       * 100.0 / COUNT(gr.GradeId) AS DECIMAL(5,1))
             ELSE 0 END                                                              AS ExcellentPct
    FROM dbo.Groups g
    LEFT JOIN dbo.Students st ON st.GroupId   = g.GroupId  AND st.IsDeleted = 0
    LEFT JOIN dbo.Grades   gr ON gr.StudentId = st.StudentId AND gr.IsDeleted = 0
    WHERE g.IsDeleted = 0 AND g.IsGraduated = 0
    GROUP BY g.GroupId, g.GroupName, g.Course
    ORDER BY g.GroupName;
END;
GO
