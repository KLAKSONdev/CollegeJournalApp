USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateAchievement
    @AchievementId  INT,
    @Title          NVARCHAR(200),
    @Category       NVARCHAR(50)  = NULL,
    @Level          NVARCHAR(50)  = NULL,
    @Description    NVARCHAR(500) = NULL,
    @AchieveDate    DATE          = NULL,
    @DocumentNumber NVARCHAR(50)  = NULL,
    @UpdatedById    INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Achievements
    SET
        Title          = @Title,
        Category       = @Category,
        Level          = @Level,
        Description    = @Description,
        AchieveDate    = @AchieveDate,
        DocumentNumber = @DocumentNumber
    WHERE AchievementId = @AchievementId
      AND IsDeleted = 0;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@UpdatedById, N'UPDATE', N'Achievements', @AchievementId);
END;
GO
