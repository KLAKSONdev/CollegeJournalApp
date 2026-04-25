USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DenyDocumentAccess
    @RequestId INT,
    @AdminId   INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DocumentAccessRequests
    SET Status       = N'Denied',
        ReviewedById = @AdminId,
        ReviewedAt   = GETDATE()
    WHERE RequestId = @RequestId;

    -- Уведомить куратора
    INSERT INTO dbo.DocumentNotifications (ToUserId, Title, Message)
    SELECT
        r.CuratorId,
        N'Запрос на доступ отклонён',
        N'Ваш запрос на доступ к документам студента ' +
        su.LastName + N' ' + su.FirstName +
        N' был отклонён администратором.'
    FROM dbo.DocumentAccessRequests r
    JOIN dbo.Students s  ON s.StudentId = r.StudentId
    JOIN dbo.Users    su ON su.UserId   = s.UserId
    WHERE r.RequestId = @RequestId;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'UPDATE', N'DocumentAccessRequests', @RequestId);
END;
GO
