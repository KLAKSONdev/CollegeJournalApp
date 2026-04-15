USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_GetGroupSubjects', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetGroupSubjects;
GO

CREATE PROCEDURE [dbo].[sp_GetGroupSubjects]
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        sub.SubjectId,
        sub.SubjectName,
        ISNULL(
            t.LastName + ' ' + t.FirstName +
            CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != ''
                 THEN ' ' + LTRIM(RTRIM(t.MiddleName)) ELSE '' END,
            '—'
        ) AS TeacherName
    FROM Subjects sub
    LEFT JOIN Teachers t ON t.TeacherId = sub.TeacherId AND t.IsDeleted = 0
    WHERE sub.GroupId   = @GroupId
      AND sub.IsDeleted = 0
    ORDER BY sub.SubjectName;
END
GO
