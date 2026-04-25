USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DownloadGeneralDoc
    @DocId        INT,
    @ViewerUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@ViewerUserId, N'VIEW', N'GeneralDocuments', @DocId);

    SELECT FileData, FileName, MimeType
    FROM dbo.GeneralDocuments
    WHERE DocId = @DocId AND IsDeleted = 0;
END;
GO
