USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_AddAchievement
    @StudentId      INT,
    @Title          NVARCHAR(200),
    @Category       NVARCHAR(50)  = NULL,
    @Level          NVARCHAR(50)  = NULL,
    @Description    NVARCHAR(500) = NULL,
    @AchieveDate    DATE          = NULL,
    @DocumentNumber NVARCHAR(50)  = NULL,
    @AddedById      INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Achievements
        (StudentId, AddedById, Title, Category, Level, Description, AchieveDate, DocumentNumber, IsDeleted)
    VALUES
        (@StudentId, @AddedById, @Title, @Category, @Level, @Description, @AchieveDate, @DocumentNumber, 0);

    DECLARE @NewId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AddedById, N'CREATE', N'Achievements', @NewId);

    SELECT @NewId AS AchievementId;
END;
GO
