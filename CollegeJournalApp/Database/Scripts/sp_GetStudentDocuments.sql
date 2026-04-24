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

    -- Аудит просмотра документов
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@ViewerUserId, N'VIEW', N'Documents', @StudentId);

    SELECT
        d.DocumentId,
        d.Title,
        ISNULL(d.DocumentType, N'—') AS DocumentType,
        ISNULL(d.FileSize, N'') AS FileSize,
        d.UploadedAt,
        ISNULL(u.LastName + N' ' + u.FirstName, N'—') AS UploadedBy,
        ISNULL(d.Description, N'') AS Description,
        ISNULL(d.FilePath, N'') AS FilePath
    FROM Documents d
    INNER JOIN Students s
           ON  s.GroupId   = d.GroupId
           AND s.StudentId = @StudentId
           AND s.IsDeleted = 0
    LEFT  JOIN Users u ON u.UserId = d.UploadedById
    WHERE d.IsDeleted = 0
    ORDER BY d.UploadedAt DESC;
END;
GO
