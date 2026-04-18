USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetAllGroups
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        g.GroupId,
        g.GroupName,
        g.Specialty,
        g.SpecialtyCode,
        g.Course,
        g.Semester,
        g.EducationForm,
        g.EducationBasis,
        g.StudentCount,
        g.IsGraduated,
        g.CuratorId,
        ISNULL(
            u.LastName + N' ' + u.FirstName +
            CASE WHEN u.MiddleName IS NOT NULL AND LTRIM(RTRIM(u.MiddleName)) != N''
                 THEN N' ' + LEFT(LTRIM(RTRIM(u.MiddleName)), 1) + N'.'
                 ELSE N'' END,
            N'—'
        ) AS CuratorName
    FROM dbo.Groups g
    LEFT JOIN dbo.Users u ON u.UserId = g.CuratorId AND u.IsDeleted = 0
    WHERE g.IsDeleted = 0
    ORDER BY g.GroupName;
END;
GO
