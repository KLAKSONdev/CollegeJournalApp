USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetTeachersAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        t.TeacherId,
        t.LastName,
        t.FirstName,
        t.MiddleName,
        FullName = t.LastName + N' ' + t.FirstName +
                   CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.MiddleName, N''))), N'') IS NOT NULL
                        THEN N' ' + t.MiddleName
                        ELSE N''
                   END,
        t.IsActive,
        SubjectCount = (
            SELECT COUNT(*)
            FROM dbo.Subjects s
            WHERE s.TeacherId = t.TeacherId
              AND s.IsDeleted = 0
        )
    FROM dbo.Teachers AS t
    WHERE t.IsDeleted = 0
    ORDER BY t.LastName, t.FirstName;
END;
GO
