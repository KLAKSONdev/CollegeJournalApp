USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_UploadStudentPhoto', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UploadStudentPhoto;
GO

CREATE PROCEDURE dbo.sp_UploadStudentPhoto
    @StudentId    INT,
    @PhotoData    VARBINARY(MAX),
    @MimeType     NVARCHAR(50),
    @UploadedById INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Students
    SET    PhotoData     = @PhotoData,
           PhotoMimeType = @MimeType
    WHERE  StudentId = @StudentId
      AND  IsDeleted = 0;
END;
GO
