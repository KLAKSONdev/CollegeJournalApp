USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteParent
    @ParentId    INT,
    @DeletedById INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Parents
    SET IsDeleted = 1
    WHERE ParentId = @ParentId;

    IF @DeletedById IS NOT NULL
        INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
        VALUES (@DeletedById, N'DELETE', N'Parents', @ParentId);
END;
GO
