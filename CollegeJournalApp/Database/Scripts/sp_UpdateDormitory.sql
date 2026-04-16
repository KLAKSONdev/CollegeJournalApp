USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_UpdateDormitory
    @DormitoryId    INT,
    @Name           NVARCHAR(200),
    @Address        NVARCHAR(300) = NULL,
    @CommandantName NVARCHAR(200) = NULL,
    @Phone          NVARCHAR(20)  = NULL,
    @TotalRooms     INT,
    @AdminId        INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Dormitories
    SET Name           = @Name,
        Address        = NULLIF(LTRIM(RTRIM(ISNULL(@Address,N''))),N''),
        CommandantName = NULLIF(LTRIM(RTRIM(ISNULL(@CommandantName,N''))),N''),
        Phone          = NULLIF(LTRIM(RTRIM(ISNULL(@Phone,N''))),N''),
        TotalRooms     = @TotalRooms
    WHERE DormitoryId = @DormitoryId AND IsDeleted = 0;
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'UPDATE', N'Dormitories', @DormitoryId);
END;
GO
