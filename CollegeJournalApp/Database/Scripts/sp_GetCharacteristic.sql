USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetCharacteristic
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sc.CharacteristicText,
        sc.UpdatedAt,
        ISNULL(u.LastName + N' ' + u.FirstName, N'') AS WrittenByName
    FROM dbo.StudentCharacteristic sc
    LEFT JOIN dbo.Users u ON u.UserId = sc.WrittenById
    WHERE sc.StudentId = @StudentId;
END;
GO
