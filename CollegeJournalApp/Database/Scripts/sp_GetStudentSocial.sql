USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_GetStudentSocial', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentSocial;
GO

CREATE PROCEDURE dbo.sp_GetStudentSocial
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        HealthGroup,
        ChronicDiseases,
        Disability,
        DisabilityGroup,
        DisabilityCertificate,
        FamilyStructure,
        FamilyType,
        HousingCondition,
        AdditionalNotes,
        SocialBenefits,
        PsychologicalFeatures,
        IsOrphan,
        IsHalfOrphan,
        IsFromLargeFamily,
        IsLowIncome,
        IsSociallyVulnerable,
        IsOnGuardianship
    FROM dbo.StudentSocialInfo
    WHERE StudentId = @StudentId;
END;
GO
