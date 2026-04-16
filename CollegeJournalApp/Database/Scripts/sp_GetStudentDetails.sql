USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_GetStudentDetails', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentDetails;
GO

CREATE PROCEDURE dbo.sp_GetStudentDetails
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.StudentCode,
        s.IsHeadman,
        s.BirthDate,
        s.BirthPlace,
        s.Gender,
        s.Citizenship,
        s.Address,
        s.SNILSNumber,
        s.PassportSeries,
        s.PassportNumber,
        s.PassportIssuedBy,
        s.PassportIssuedDate,
        s.PreviousSchool,
        s.PreviousSchoolType,
        s.StudyBasis,
        s.RoomNumber,
        s.PhotoData,
        s.PhotoMimeType,
        s.EnrollmentDate,
        u.LastName + ' ' + u.FirstName + ISNULL(' ' + u.MiddleName, '') AS FullName,
        u.LastName,
        u.FirstName,
        ISNULL(u.MiddleName, '') AS MiddleName,
        ISNULL(u.Phone, N'—') AS Phone,
        ISNULL(u.Email, N'—') AS Email,
        g.GroupName,
        d.Name AS DormitoryName
    FROM Students s
    INNER JOIN Users u ON u.UserId = s.UserId
    INNER JOIN Groups g ON g.GroupId = s.GroupId
    LEFT  JOIN Dormitories d ON d.DormitoryId = s.DormitoryId AND d.IsDeleted = 0
    WHERE s.StudentId = @StudentId
      AND s.IsDeleted = 0;
END;
GO
