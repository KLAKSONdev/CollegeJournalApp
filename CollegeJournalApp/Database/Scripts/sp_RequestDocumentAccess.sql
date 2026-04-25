USE CollegeJournal;
GO

-- Куратор запрашивает доступ к личным документам студента.
-- Если уже есть активный (Pending) запрос — возвращает его, не дублирует.
CREATE OR ALTER PROCEDURE dbo.sp_RequestDocumentAccess
    @CuratorId INT,
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Уже есть ожидающий запрос — вернуть его
    IF EXISTS (
        SELECT 1 FROM dbo.DocumentAccessRequests
        WHERE CuratorId = @CuratorId AND StudentId = @StudentId AND Status = N'Pending'
    )
    BEGIN
        SELECT RequestId, Status, ExpiresAt
        FROM dbo.DocumentAccessRequests
        WHERE CuratorId = @CuratorId AND StudentId = @StudentId AND Status = N'Pending';
        RETURN;
    END

    -- Создать новый запрос
    INSERT INTO dbo.DocumentAccessRequests (CuratorId, StudentId)
    VALUES (@CuratorId, @StudentId);

    DECLARE @NewId INT = SCOPE_IDENTITY();

    -- Уведомить всех активных администраторов
    INSERT INTO dbo.DocumentNotifications (ToUserId, Title, Message)
    SELECT
        u.UserId,
        N'Запрос на доступ к документам',
        N'Куратор ' + cu.LastName + N' ' + cu.FirstName +
        N' запрашивает доступ к документам студента ' +
        su.LastName + N' ' + su.FirstName + N'.'
    FROM dbo.Users u
    JOIN dbo.Roles r ON r.RoleId = u.RoleId AND r.RoleName = N'Admin'
    JOIN dbo.Users cu ON cu.UserId = @CuratorId
    JOIN dbo.Students s ON s.StudentId = @StudentId
    JOIN dbo.Users su ON su.UserId = s.UserId
    WHERE u.IsDeleted = 0 AND u.IsActive = 1;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@CuratorId, N'CREATE', N'DocumentAccessRequests', @NewId);

    SELECT @NewId AS RequestId, N'Pending' AS Status, NULL AS ExpiresAt;
END;
GO
