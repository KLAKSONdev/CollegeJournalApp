USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetLessonNotes
    @GroupId   INT,
    @SubjectId INT,
    @Year      INT,
    @Month     INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DAY(LessonDate)  AS DayNum,
        NoteText
    FROM dbo.LessonNotes
    WHERE
        GroupId   = @GroupId
        AND SubjectId = @SubjectId
        AND YEAR(LessonDate)  = @Year
        AND MONTH(LessonDate) = @Month
        AND LEN(LTRIM(RTRIM(NoteText))) > 0
    ORDER BY LessonDate;
END;
GO
