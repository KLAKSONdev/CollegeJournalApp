USE CollegeJournal;
GO

-- Непрочитанные уведомления пользователя по документам.
CREATE OR ALTER PROCEDURE dbo.sp_GetDocumentNotifications
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT NotifId, Title, Message, CreatedAt
    FROM dbo.DocumentNotifications
    WHERE ToUserId = @UserId
      AND IsRead   = 0
    ORDER BY CreatedAt DESC;
END;
GO

-- ────────────────────────────────────────────────────────────────

CREATE OR ALTER PROCEDURE dbo.sp_MarkDocumentNotifsRead
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DocumentNotifications
    SET IsRead = 1
    WHERE ToUserId = @UserId AND IsRead = 0;
END;
GO
