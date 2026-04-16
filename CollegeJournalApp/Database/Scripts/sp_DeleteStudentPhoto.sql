USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_DeleteStudentPhoto', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteStudentPhoto;
GO

CREATE PROCEDURE dbo.sp_DeleteStudentPhoto
    @StudentId   INT,
    @DeletedById INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Students
    SET    PhotoData     = NULL,
           PhotoMimeType = NULL
    WHERE  StudentId = @StudentId
      AND  IsDeleted = 0;
END;
GO
