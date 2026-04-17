USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateUser
    @UserId     INT,
    @Login      NVARCHAR(50),
    @RoleName   NVARCHAR(20),
    @LastName   NVARCHAR(50),
    @FirstName  NVARCHAR(50),
    @MiddleName NVARCHAR(50)  = NULL,
    @Phone      NVARCHAR(20)  = NULL,
    @Email      NVARCHAR(100) = NULL,
    @AdminId    INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Проверка уникальности логина (кроме самого себя)
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Login = @Login AND UserId <> @UserId AND IsDeleted = 0)
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

    UPDATE dbo.Users SET
        Login      = @Login,
        RoleId     = @RoleId,
        LastName   = @LastName,
        FirstName  = @FirstName,
        MiddleName = @MiddleName,
        Phone      = @Phone,
        Email      = @Email
    WHERE UserId = @UserId AND IsDeleted = 0;

    -- Аудит
    IF @AdminId IS NOT NULL
    BEGIN
        INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
        VALUES (@AdminId, N'UPDATE', N'Users', @UserId);
    END
END;
GO
