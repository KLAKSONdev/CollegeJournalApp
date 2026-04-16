USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddTeacher
    @LastName   NVARCHAR(100),
    @FirstName  NVARCHAR(100),
    @MiddleName NVARCHAR(100) = NULL,
    @IsActive   BIT = 1,
    @AdminId    INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Teachers (LastName, FirstName, MiddleName, IsActive, IsDeleted)
    VALUES (@LastName, @FirstName, NULLIF(LTRIM(RTRIM(ISNULL(@MiddleName, N''))), N''), @IsActive, 0);
    DECLARE @NewId INT = SCOPE_IDENTITY();
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'CREATE', N'Teachers', @NewId);
    SELECT @NewId AS TeacherId;
END;
GO
