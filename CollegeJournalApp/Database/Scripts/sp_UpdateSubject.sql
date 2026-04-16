USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_UpdateSubject
    @SubjectId      INT,
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
    UPDATE dbo.Subjects
    SET GroupId        = @GroupId,
        TeacherId      = NULLIF(@TeacherId,0),
        SubjectName    = @SubjectName,
        HoursTotal     = @HoursTotal,
        HoursLecture   = @HoursLecture,
        HoursPractice  = @HoursPractice,
        HoursLab       = @HoursLab,
        HoursSelfStudy = @HoursSelfStudy,
        Semester       = @Semester,
        ControlType    = @ControlType
    WHERE SubjectId = @SubjectId AND IsDeleted = 0;
    INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
    VALUES (@AdminId, N'UPDATE', N'Subjects', @SubjectId);
END;
GO
