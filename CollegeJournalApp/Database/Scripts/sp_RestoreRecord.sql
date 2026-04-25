USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- sp_RestoreRecord
-- Восстанавливает мягко удалённую запись из корзины.
-- Разрешено только администратору (проверка на стороне UI).
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_RestoreRecord
    @TableName    NVARCHAR(50),
    @RecordId     INT,
    @RestoredById INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected INT = 0;

    IF @TableName = N'Students'
    BEGIN
        UPDATE dbo.Students
        SET    IsDeleted = 0
        WHERE  StudentId = @RecordId AND IsDeleted = 1;
        SET @Affected = @@ROWCOUNT;

        -- Восстанавливаем учётную запись пользователя вместе со студентом
        IF @Affected > 0
            UPDATE dbo.Users
            SET    IsDeleted = 0, IsActive = 1
            WHERE  UserId = (SELECT UserId FROM dbo.Students WHERE StudentId = @RecordId);
    END
    ELSE IF @TableName = N'Documents'
    BEGIN
        UPDATE dbo.Documents
        SET    IsDeleted = 0
        WHERE  DocumentId = @RecordId AND IsDeleted = 1;
        SET @Affected = @@ROWCOUNT;
    END
    ELSE IF @TableName = N'Parents'
    BEGIN
        UPDATE dbo.Parents
        SET    IsDeleted = 0
        WHERE  ParentId = @RecordId AND IsDeleted = 1;
        SET @Affected = @@ROWCOUNT;
    END

    -- Аудит (только если запись действительно изменилась)
    IF @Affected > 0
        INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
        VALUES (@RestoredById, N'RESTORE', @TableName, @RecordId);

    SELECT @Affected AS RestoredCount;
END;
GO
