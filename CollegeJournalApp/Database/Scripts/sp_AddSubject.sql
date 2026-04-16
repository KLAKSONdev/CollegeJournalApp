USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_AddSubject
    @GroupId        INT,
    @TeacherId      INT = NULL,
    @SubjectName    NVARCHAR(200),
    @HoursTotal     INT = 0,
    @HoursLecture   INT = 0,
    @HoursPractice  INT = 0,
    @HoursLab       INT = 0,
    @HoursSelfStudy INT = 0,
    @Semester       NVARCHAR(5),
    @ControlType    NVARCHAR(50),
    @AdminId        INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Subjects
        (GroupId, TeacherId, SubjectName, HoursTotal, HoursLecture, HoursPractice,
         HoursLab, HoursSelfStudy, Semester, ControlType, IsDeleted)
    VALUES
        (@GroupId, NULLIF(@TeacherId,0), @SubjectName, @HoursTotal, @HoursLecture, @HoursPractice,
         @HoursLab, @HoursSelfStudy, @Semester, @ControlType, 0);
    DECLARE @NewId INT = SCOPE_IDENTITY();
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'CREATE', N'Subjects', @NewId);
    SELECT @NewId AS SubjectId;
END;
GO
