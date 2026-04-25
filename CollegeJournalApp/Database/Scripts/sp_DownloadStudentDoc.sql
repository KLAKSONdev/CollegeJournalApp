USE CollegeJournal;
GO

-- Возвращает бинарное содержимое одного личного документа студента.
CREATE OR ALTER PROCEDURE dbo.sp_DownloadStudentDoc
    @DocId        INT,
    @ViewerUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@ViewerUserId, N'VIEW', N'StudentPersonalDocuments', @DocId);

    SELECT FileData, FileName, MimeType
    FROM dbo.StudentPersonalDocuments
    WHERE DocId = @DocId AND IsDeleted = 0;
END;
GO
