USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetSubjectsAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        s.SubjectId,
        s.SubjectName,
        s.GroupId,
        g.GroupName,
        s.TeacherId,
        TeacherName = ISNULL(
            t.LastName + N' ' + t.FirstName +
            CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.MiddleName, N''))), N'') IS NOT NULL
                 THEN N' ' + t.MiddleName
                 ELSE N''
            END,
            N'—'
        ),
        s.HoursTotal,
        s.HoursLecture,
        s.HoursPractice,
        s.HoursLab,
        s.HoursSelfStudy,
        s.Semester,
        s.ControlType
    FROM dbo.Subjects AS s
    INNER JOIN dbo.Groups AS g
        ON g.GroupId = s.GroupId
       AND g.IsDeleted = 0
    LEFT JOIN dbo.Teachers AS t
        ON t.TeacherId = s.TeacherId
       AND t.IsDeleted = 0
    WHERE s.IsDeleted = 0
    ORDER BY g.GroupName, s.SubjectName;
END;
GO
