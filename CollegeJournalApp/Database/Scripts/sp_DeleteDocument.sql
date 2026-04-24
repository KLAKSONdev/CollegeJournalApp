USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteDocument
    @DocumentId  INT,
    @DeletedById INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Documents
    SET IsDeleted = 1
    WHERE DocumentId = @DocumentId;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@DeletedById, N'DELETE', N'Documents', @DocumentId);
END;
GO
