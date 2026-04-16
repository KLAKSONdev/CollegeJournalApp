USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_UpdateTeacher
    @TeacherId  INT,
    @LastName   NVARCHAR(100),
    @FirstName  NVARCHAR(100),
    @MiddleName NVARCHAR(100) = NULL,
    @IsActive   BIT,
    @AdminId    INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Teachers
    SET LastName   = @LastName,
        FirstName  = @FirstName,
        MiddleName = NULLIF(LTRIM(RTRIM(ISNULL(@MiddleName,N''))),N''),
        IsActive   = @IsActive
    WHERE TeacherId = @TeacherId AND IsDeleted = 0;
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'UPDATE', N'Teachers', @TeacherId);
END;
GO
