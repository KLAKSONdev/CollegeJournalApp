USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddTeacher
    @LastName     NVARCHAR(100),
    @FirstName    NVARCHAR(100),
    @MiddleName   NVARCHAR(100) = NULL,
    @IsActive     BIT           = 1,
    @Login        NVARCHAR(50)  = NULL,
    @PasswordHash NVARCHAR(64)  = NULL,
    @Phone        NVARCHAR(20)  = NULL,
    @Email        NVARCHAR(100) = NULL,
    @AdminId      INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @NewUserId INT = NULL;

        -- Создаём учётную запись если передан логин
        IF @Login IS NOT NULL AND LEN(LTRIM(RTRIM(@Login))) > 0
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.Users WHERE Login = @Login AND IsDeleted = 0)
            BEGIN
                ROLLBACK;
                RAISERROR(N'Пользователь с таким логином уже существует. Выберите другой логин.', 16, 1);
                RETURN;
            END

            DECLARE @RoleId INT;
            SELECT @RoleId = RoleId FROM dbo.Roles WHERE RoleName = N'Teacher';
            -- Если роль Teacher не создана — fallback на Curator
            IF @RoleId IS NULL
                SELECT @RoleId = RoleId FROM dbo.Roles WHERE RoleName = N'Curator';

            INSERT INTO dbo.Users
                (Login, PasswordHash, RoleId, LastName, FirstName, MiddleName, Phone, Email, IsActive, CreatedAt)
            VALUES
                (@Login, @PasswordHash, @RoleId,
                 @LastName, @FirstName,
                 NULLIF(LTRIM(RTRIM(ISNULL(@MiddleName, N''))), N''),
                 @Phone, @Email, 1, GETDATE());

            SET @NewUserId = SCOPE_IDENTITY();
        END

        -- Создаём запись преподавателя
        INSERT INTO dbo.Teachers (LastName, FirstName, MiddleName, IsActive, UserId, IsDeleted)
        VALUES (
            @LastName,
            @FirstName,
            NULLIF(LTRIM(RTRIM(ISNULL(@MiddleName, N''))), N''),
            @IsActive,
            @NewUserId,
            0
        );

        DECLARE @NewTeacherId INT = SCOPE_IDENTITY();

        INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
        VALUES (@AdminId, N'CREATE', N'Teachers', @NewTeacherId);

        COMMIT;
        SELECT @NewTeacherId AS TeacherId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
