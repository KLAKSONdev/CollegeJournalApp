USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DeleteAchievement
    @AchievementId INT,
    @DeletedById   INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Achievements
    SET IsDeleted = 1
    WHERE AchievementId = @AchievementId;

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@DeletedById, N'SOFT_DELETE', N'Achievements', @AchievementId);
END;
GO
