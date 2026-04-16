USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetTeachers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        t.TeacherId,
        t.LastName + N' ' + t.FirstName +
            CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != ''
                 THEN N' ' + LTRIM(RTRIM(t.MiddleName)) ELSE N'' END AS FullName,
        t.LastName, t.FirstName, ISNULL(t.MiddleName, N'') AS MiddleName,
        t.IsActive,
        (SELECT COUNT(*) FROM dbo.Subjects WHERE TeacherId = t.TeacherId AND IsDeleted = 0) AS SubjectCount
    FROM dbo.Teachers t
    WHERE t.IsDeleted = 0
    ORDER BY t.LastName, t.FirstName;
END;
GO
