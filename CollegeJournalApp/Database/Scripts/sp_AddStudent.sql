USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddStudent
    @Login          NVARCHAR(50),
    @PasswordHash   NVARCHAR(64),
    @LastName       NVARCHAR(50),
    @FirstName      NVARCHAR(50),
    @MiddleName     NVARCHAR(50)  = NULL,
    @Phone          NVARCHAR(20)  = NULL,
    @Email          NVARCHAR(100) = NULL,
    @GroupId        INT,
    @BirthDate      DATE,
    @Gender         NVARCHAR(10)  = N'Мужской',
    @StudyBasis     NVARCHAR(20)  = N'Бюджет',
    @StudentCode    NVARCHAR(30)  = NULL,
    @EnrollmentDate DATE          = NULL,
    @AddedById      INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Проверка уникальности логина
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Login = @Login AND IsDeleted = 0)
        BEGIN
            ROLLBACK;
            SELECT 0 AS StudentId, N'Пользователь с таким логином уже существует. Выберите другой логин.' AS Message;
            RETURN;
        END

        -- Проверка уникальности зачётной книжки
        IF @StudentCode IS NOT NULL AND EXISTS (
            SELECT 1 FROM dbo.Students WHERE StudentCode = @StudentCode AND IsDeleted = 0
        )
        BEGIN
            ROLLBACK;
            SELECT 0 AS StudentId, N'Студент с таким номером зачётной книжки уже существует.' AS Message;
            RETURN;
        END

        -- Получаем RoleId для Student
        DECLARE @RoleId INT;
        SELECT @RoleId = RoleId FROM dbo.Roles WHERE RoleName = N'Student';
        IF @RoleId IS NULL
        BEGIN
            ROLLBACK;
            SELECT 0 AS StudentId, N'Роль Student не найдена в базе данных.' AS Message;
            RETURN;
        END

        -- Создаём учётную запись
        INSERT INTO dbo.Users
            (Login, PasswordHash, RoleId, LastName, FirstName, MiddleName, Phone, Email, IsActive, CreatedAt)
        VALUES
            (@Login, @PasswordHash, @RoleId, @LastName, @FirstName, @MiddleName, @Phone, @Email, 1, GETDATE());

        DECLARE @NewUserId INT = SCOPE_IDENTITY();

        -- Создаём запись студента
        INSERT INTO dbo.Students
            (UserId, GroupId, StudentCode, BirthDate, Gender, StudyBasis, EnrollmentDate, IsDeleted)
        VALUES
            (@NewUserId, @GroupId, @StudentCode, @BirthDate, @Gender, @StudyBasis,
             ISNULL(@EnrollmentDate, GETDATE()), 0);

        DECLARE @NewStudentId INT = SCOPE_IDENTITY();

        -- Аудит
        IF @AddedById IS NOT NULL
            INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
            VALUES (@AddedById, N'CREATE', N'Students', @NewStudentId);

        COMMIT;

        SELECT @NewStudentId AS StudentId, N'OK' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SELECT 0 AS StudentId, ERROR_MESSAGE() AS Message;
    END CATCH
END;
GO
