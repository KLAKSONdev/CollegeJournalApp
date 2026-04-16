USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_AddAcademicYear
    @Title     NVARCHAR(20),
    @StartDate DATE,
    @EndDate   DATE,
    @IsCurrent BIT = 0,
    @AdminId   INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    IF @IsCurrent = 1
        UPDATE dbo.AcademicYears SET IsCurrent = 0 WHERE IsDeleted = 0;
    INSERT INTO dbo.AcademicYears (Title, StartDate, EndDate, IsCurrent, IsDeleted)
    VALUES (@Title, @StartDate, @EndDate, @IsCurrent, 0);
    DECLARE @NewId INT = SCOPE_IDENTITY();
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'CREATE', N'AcademicYears', @NewId);
    COMMIT;
    SELECT @NewId AS YearId;
END;
GO
