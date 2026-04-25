USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteStudentPersonalDoc
    @DocId       INT,
    @DeletedById INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.StudentPersonalDocuments
    SET IsDeleted = 1
    WHERE DocId = @DocId;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@DeletedById, N'SOFT_DELETE', N'StudentPersonalDocuments', @DocId);
END;
GO
