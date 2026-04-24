USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveStudentSocial
    @StudentId             INT,
    @HealthGroup           NVARCHAR(2)   = NULL,
    @ChronicDiseases       NVARCHAR(300) = NULL,
    @Disability            NVARCHAR(200) = NULL,
    @DisabilityGroup       NVARCHAR(3)   = NULL,
    @DisabilityCertificate NVARCHAR(50)  = NULL,
    @FamilyStructure       NVARCHAR(30)  = NULL,
    @FamilyType            NVARCHAR(50)  = NULL,
    @HousingCondition      NVARCHAR(50)  = NULL,
    @AdditionalNotes       NVARCHAR(500) = NULL,
    @SocialBenefits        NVARCHAR(300) = NULL,
    @PsychologicalFeatures NVARCHAR(300) = NULL,
    @IsOrphan              BIT           = 0,
    @IsHalfOrphan          BIT           = 0,
    @IsFromLargeFamily     BIT           = 0,
    @IsLowIncome           BIT           = 0,
    @IsSociallyVulnerable  BIT           = 0,
    @IsOnGuardianship      BIT           = 0,
    @UpdatedById           INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.StudentSocialInfo WHERE StudentId = @StudentId)
    BEGIN
        UPDATE dbo.StudentSocialInfo
        SET HealthGroup           = @HealthGroup,
            ChronicDiseases       = @ChronicDiseases,
            Disability            = @Disability,
            DisabilityGroup       = @DisabilityGroup,
            DisabilityCertificate = @DisabilityCertificate,
            FamilyStructure       = @FamilyStructure,
            FamilyType            = @FamilyType,
            HousingCondition      = @HousingCondition,
            AdditionalNotes       = @AdditionalNotes,
            SocialBenefits        = @SocialBenefits,
            PsychologicalFeatures = @PsychologicalFeatures,
            IsOrphan              = @IsOrphan,
            IsHalfOrphan          = @IsHalfOrphan,
            IsFromLargeFamily     = @IsFromLargeFamily,
            IsLowIncome           = @IsLowIncome,
            IsSociallyVulnerable  = @IsSociallyVulnerable,
            IsOnGuardianship      = @IsOnGuardianship
        WHERE StudentId = @StudentId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.StudentSocialInfo
            (StudentId, HealthGroup, ChronicDiseases, Disability, DisabilityGroup,
             DisabilityCertificate, FamilyStructure, FamilyType, HousingCondition,
             AdditionalNotes, SocialBenefits, PsychologicalFeatures,
             IsOrphan, IsHalfOrphan, IsFromLargeFamily, IsLowIncome,
             IsSociallyVulnerable, IsOnGuardianship)
        VALUES
            (@StudentId, @HealthGroup, @ChronicDiseases, @Disability, @DisabilityGroup,
             @DisabilityCertificate, @FamilyStructure, @FamilyType, @HousingCondition,
             @AdditionalNotes, @SocialBenefits, @PsychologicalFeatures,
             @IsOrphan, @IsHalfOrphan, @IsFromLargeFamily, @IsLowIncome,
             @IsSociallyVulnerable, @IsOnGuardianship);
    END

    IF @UpdatedById IS NOT NULL
        INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
        VALUES (@UpdatedById, N'UPDATE', N'StudentSocialInfo', @StudentId);
END;
GO
