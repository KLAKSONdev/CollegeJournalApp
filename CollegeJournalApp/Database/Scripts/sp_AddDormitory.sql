USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_AddDormitory
    @Name           NVARCHAR(200),
    @Address        NVARCHAR(300) = NULL,
    @CommandantName NVARCHAR(200) = NULL,
    @Phone          NVARCHAR(20)  = NULL,
    @TotalRooms     INT = 0,
    @AdminId        INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Dormitories (Name, Address, CommandantName, Phone, TotalRooms, IsDeleted)
    VALUES (@Name,
            NULLIF(LTRIM(RTRIM(ISNULL(@Address,N''))),N''),
            NULLIF(LTRIM(RTRIM(ISNULL(@CommandantName,N''))),N''),
            NULLIF(LTRIM(RTRIM(ISNULL(@Phone,N''))),N''),
            @TotalRooms, 0);
    DECLARE @NewId INT = SCOPE_IDENTITY();
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'CREATE', N'Dormitories', @NewId);
    SELECT @NewId AS DormitoryId;
END;
GO
