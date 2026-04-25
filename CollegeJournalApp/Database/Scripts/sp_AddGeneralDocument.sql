USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddGeneralDocument
    @Title        NVARCHAR(200),
    @DocType      NVARCHAR(50)   = N'Прочее',
    @FileName     NVARCHAR(255),
    @FileData     VARBINARY(MAX),
    @MimeType     NVARCHAR(100)  = NULL,
    @FileSizeKB   INT            = NULL,
    @Description  NVARCHAR(500)  = NULL,
    @UploadedById INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.GeneralDocuments
        (Title, DocType, FileName, FileData, MimeType, FileSizeKB, Description, UploadedById)
    VALUES
        (@Title, @DocType, @FileName, @FileData, @MimeType, @FileSizeKB, @Description, @UploadedById);

    DECLARE @NewId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@UploadedById, N'CREATE', N'GeneralDocuments', @NewId);

    SELECT @NewId AS DocId;
END;
GO
