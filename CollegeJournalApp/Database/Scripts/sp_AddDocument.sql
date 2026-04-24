USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddDocument
    @StudentId    INT,
    @UploadedById INT,
    @Title        NVARCHAR(200),
    @DocumentType NVARCHAR(50)  = NULL,
    @FilePath     NVARCHAR(500) = NULL,
    @FileSize     NVARCHAR(20)  = NULL,
    @Description  NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @GroupId INT;
    SELECT @GroupId = GroupId
    FROM dbo.Students
    WHERE StudentId = @StudentId AND IsDeleted = 0;

    IF @GroupId IS NULL
    BEGIN
        RAISERROR(N'Студент не найден.', 16, 1);
        RETURN;
    END

    INSERT INTO dbo.Documents
        (GroupId, UploadedById, Title, DocumentType, FilePath, FileSize, UploadedAt, Description, IsDeleted)
    VALUES
        (@GroupId, @UploadedById, @Title, @DocumentType, @FilePath, @FileSize, GETDATE(), @Description, 0);

    DECLARE @NewId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@UploadedById, N'CREATE', N'Documents', @NewId);
END;
GO
