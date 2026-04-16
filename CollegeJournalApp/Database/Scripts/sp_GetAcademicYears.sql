USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetAcademicYears
AS
BEGIN
    SET NOCOUNT ON;
    SELECT YearId, Title, StartDate, EndDate, IsCurrent
    FROM dbo.AcademicYears
    WHERE IsDeleted = 0
    ORDER BY StartDate DESC;
END;
GO
