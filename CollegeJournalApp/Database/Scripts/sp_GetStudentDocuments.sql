USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_GetStudentDocuments', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentDocuments;
GO

CREATE PROCEDURE dbo.sp_GetStudentDocuments
    @StudentId    INT,
    @ViewerUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Documents are stored per group, not per student.
    -- Join Students to find the student's group, then return all group documents.
    SELECT
        d.Title,
        d.DocumentType,
        ISNULL(CAST(d.FileSize AS NVARCHAR(20)), N'') AS FileSize,
        d.UploadedAt,
        ISNULL(u.LastName + N' ' + u.FirstName, N'—') AS UploadedBy,
        ISNULL(d.Description, N'') AS Description
    FROM Documents d
    INNER JOIN Students s
           ON  s.GroupId    = d.GroupId
           AND s.StudentId  = @StudentId
           AND s.IsDeleted  = 0
    LEFT  JOIN Users u ON u.UserId = d.UploadedById
    WHERE d.IsDeleted = 0
    ORDER BY d.UploadedAt DESC;
END;
GO
