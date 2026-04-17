USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddUser
    @Login        NVARCHAR(50),
    @PasswordHash NVARCHAR(64),
    @RoleName     NVARCHAR(20),
    @LastName     NVARCHAR(50),
    @FirstName    NVARCHAR(50),
    @MiddleName   NVARCHAR(50)  = NULL,
    @Phone        NVARCHAR(20)  = NULL,
    @Email        NVARCHAR(100) = NULL,
    @AdminId      INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Проверка уникальности логина
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Login = @Login AND IsDeleted = 0)
    BEGIN
        RAISERROR(N'Пользователь с таким логином уже существует.', 16, 1);
        RETURN;
    END

    -- Получаем RoleId
    DECLARE @RoleId INT;
    SELECT @RoleId = RoleId FROM dbo.Roles WHERE RoleName = @RoleName;
    IF @RoleId IS NULL
    BEGIN
        RAISERROR(N'Роль не найдена: %s', 16, 1, @RoleName);
        RETURN;
    END

    INSERT INTO dbo.Users
        (Login, PasswordHash, RoleId, LastName, FirstName, MiddleName, Phone, Email, IsActive, CreatedAt)
    VALUES
        (@Login, @PasswordHash, @RoleId, @LastName, @FirstName, @MiddleName, @Phone, @Email, 1, GETDATE());

    DECLARE @NewUserId INT = SCOPE_IDENTITY();

    -- Аудит
    IF @AdminId IS NOT NULL
    BEGIN
        INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
        VALUES (@AdminId, N'CREATE', N'Users', @NewUserId);
    END
END;
GO
