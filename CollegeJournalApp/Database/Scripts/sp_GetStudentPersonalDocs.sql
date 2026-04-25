USE CollegeJournal;
GO

-- Список личных документов студента (без FileData — только метаданные).
CREATE OR ALTER PROCEDURE dbo.sp_GetStudentPersonalDocs
    @StudentId    INT,
    @ViewerUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@ViewerUserId, N'VIEW', N'StudentPersonalDocuments', @StudentId);

    SELECT
        d.DocId,
        d.Title,
        d.DocType,
        d.FileName,
        d.MimeType,
        ISNULL(d.FileSizeKB, 0)                              AS FileSizeKB,
        ISNULL(d.Description, N'')                           AS Description,
        d.UploadedAt,
        ISNULL(u.LastName + N' ' + u.FirstName, N'—')        AS UploadedByName
    FROM dbo.StudentPersonalDocuments d
    LEFT JOIN dbo.Users u ON u.UserId = d.UploadedById
    WHERE d.StudentId = @StudentId
      AND d.IsDeleted = 0
    ORDER BY d.UploadedAt DESC;
END;
GO
