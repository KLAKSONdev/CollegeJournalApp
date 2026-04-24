USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateStudentPersonal
    @StudentId          INT,
    @BirthPlace         NVARCHAR(100) = NULL,
    @Citizenship        NVARCHAR(50)  = NULL,
    @Address            NVARCHAR(300) = NULL,
    @SNILSNumber        NVARCHAR(30)  = NULL,
    @PassportSeries     NVARCHAR(4)   = NULL,
    @PassportNumber     NVARCHAR(6)   = NULL,
    @PassportIssuedBy   NVARCHAR(200) = NULL,
    @PassportIssuedDate DATE          = NULL,
    @PreviousSchool     NVARCHAR(200) = NULL,
    @PreviousSchoolType NVARCHAR(20)  = NULL,
    @UpdatedById        INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Students
    SET BirthPlace         = @BirthPlace,
        Citizenship        = @Citizenship,
        Address            = @Address,
        SNILSNumber        = @SNILSNumber,
        PassportSeries     = @PassportSeries,
        PassportNumber     = @PassportNumber,
        PassportIssuedBy   = @PassportIssuedBy,
        PassportIssuedDate = @PassportIssuedDate,
        PreviousSchool     = @PreviousSchool,
        PreviousSchoolType = @PreviousSchoolType
    WHERE StudentId = @StudentId
      AND IsDeleted  = 0;

    IF @UpdatedById IS NOT NULL
        INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
        VALUES (@UpdatedById, N'UPDATE', N'Students', @StudentId);
END;
GO
