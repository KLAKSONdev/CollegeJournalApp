USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ApproveDocumentAccess
    @RequestId INT,
    @AdminId   INT,
    @ExpiresAt DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DocumentAccessRequests
    SET Status       = N'Approved',
        ReviewedById = @AdminId,
        ReviewedAt   = GETDATE(),
        ExpiresAt    = @ExpiresAt
    WHERE RequestId = @RequestId;

    -- Уведомить куратора
    INSERT INTO dbo.DocumentNotifications (ToUserId, Title, Message)
    SELECT
        r.CuratorId,
        N'Доступ к документам одобрен',
        N'Ваш запрос на доступ к документам студента ' +
        su.LastName + N' ' + su.FirstName +
        N' одобрен до ' + CONVERT(NVARCHAR, @ExpiresAt, 104) + N'.'
    FROM dbo.DocumentAccessRequests r
    JOIN dbo.Students s  ON s.StudentId = r.StudentId
    JOIN dbo.Users    su ON su.UserId   = s.UserId
    WHERE r.RequestId = @RequestId;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'UPDATE', N'DocumentAccessRequests', @RequestId);
END;
GO
