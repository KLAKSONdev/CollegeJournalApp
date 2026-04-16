USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_UpdateAcademicYear
    @YearId    INT,
    @Title     NVARCHAR(20),
    @StartDate DATE,
    @EndDate   DATE,
    @IsCurrent BIT,
    @AdminId   INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    IF @IsCurrent = 1
        UPDATE dbo.AcademicYears SET IsCurrent = 0 WHERE IsDeleted = 0 AND YearId != @YearId;
    UPDATE dbo.AcademicYears
    SET Title = @Title, StartDate = @StartDate, EndDate = @EndDate, IsCurrent = @IsCurrent
    WHERE YearId = @YearId AND IsDeleted = 0;
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'UPDATE', N'AcademicYears', @YearId);
    COMMIT;
END;
GO
