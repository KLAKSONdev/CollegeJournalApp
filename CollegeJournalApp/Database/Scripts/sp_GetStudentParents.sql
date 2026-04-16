USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_GetStudentParents', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentParents;
GO

CREATE PROCEDURE dbo.sp_GetStudentParents
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Relation,
        p.LastName,
        p.FirstName,
        p.MiddleName,
        p.Phone,
        p.WorkPhone,
        p.Workplace,
        p.Position,
        p.Education
    FROM Parents p
    WHERE p.StudentId = @StudentId
      AND p.IsDeleted = 0
    ORDER BY p.IsMainContact DESC, p.ParentId;
END;
GO
