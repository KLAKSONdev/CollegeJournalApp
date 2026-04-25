USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveCharacteristic
    @StudentId         INT,
    @CharacteristicText NVARCHAR(MAX),
    @WrittenById       INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.StudentCharacteristic WHERE StudentId = @StudentId)
    BEGIN
        UPDATE dbo.StudentCharacteristic
        SET
            CharacteristicText = @CharacteristicText,
            WrittenById        = @WrittenById,
            UpdatedAt          = GETDATE()
        WHERE StudentId = @StudentId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.StudentCharacteristic
            (StudentId, CharacteristicText, WrittenById, UpdatedAt)
        VALUES
            (@StudentId, @CharacteristicText, @WrittenById, GETDATE());
    END

    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@WrittenById, N'UPDATE', N'StudentCharacteristic', @StudentId);
END;
GO
