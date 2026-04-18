USE CollegeJournal;
GO

-- Назначить/снять куратора группы.
-- @TeacherUserId = NULL → снять куратора
CREATE OR ALTER PROCEDURE dbo.sp_AssignCurator
    @GroupId       INT,
    @TeacherUserId INT = NULL,   -- UserId преподавателя (NULL = снять)
    @AdminId       INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CuratorRoleId INT, @TeacherRoleId INT;
        SELECT @CuratorRoleId = RoleId FROM dbo.Roles WHERE RoleName = N'Curator';
        SELECT @TeacherRoleId = RoleId FROM dbo.Roles WHERE RoleName = N'Teacher';

        -- Текущий куратор этой группы
        DECLARE @CurrentCuratorUserId INT;
        SELECT @CurrentCuratorUserId = CuratorId FROM dbo.Groups WHERE GroupId = @GroupId;

        -- Если снимаем текущего куратора — вернуть ему роль Teacher
        -- (только если он больше не куратор ни одной другой группы)
        IF @CurrentCuratorUserId IS NOT NULL
           AND @CurrentCuratorUserId <> ISNULL(@TeacherUserId, -1)
        BEGIN
            DECLARE @OtherGroups INT;
            SELECT @OtherGroups = COUNT(*)
            FROM dbo.Groups
            WHERE CuratorId = @CurrentCuratorUserId
              AND GroupId   <> @GroupId
              AND IsDeleted  = 0;

            IF @OtherGroups = 0
                UPDATE dbo.Users
                SET RoleId = @TeacherRoleId
                WHERE UserId = @CurrentCuratorUserId
                  AND RoleId = @CuratorRoleId; -- меняем только если был Curator
        END

        -- Проверка: преподаватель уже куратор другой группы?
        IF @TeacherUserId IS NOT NULL
        BEGIN
            DECLARE @AlreadyCuratorOf INT;
            SELECT @AlreadyCuratorOf = GroupId
            FROM dbo.Groups
            WHERE CuratorId = @TeacherUserId
              AND GroupId   <> @GroupId
              AND IsDeleted  = 0;

            IF @AlreadyCuratorOf IS NOT NULL
            BEGIN
                DECLARE @OtherGroupName NVARCHAR(50);
                SELECT @OtherGroupName = GroupName FROM dbo.Groups WHERE GroupId = @AlreadyCuratorOf;
                ROLLBACK;
                RAISERROR(N'Этот преподаватель уже является куратором группы «%s». Один преподаватель может быть куратором только одной группы.', 16, 1, @OtherGroupName);
                RETURN;
            END
        END

        -- Назначаем нового куратора
        UPDATE dbo.Groups
        SET CuratorId = @TeacherUserId
        WHERE GroupId = @GroupId;

        -- Новому куратору ставим роль Curator
        IF @TeacherUserId IS NOT NULL
            UPDATE dbo.Users
            SET RoleId = @CuratorRoleId
            WHERE UserId = @TeacherUserId;

        -- Аудит
        INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
        VALUES (@AdminId, N'UPDATE', N'Groups', @GroupId);

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
