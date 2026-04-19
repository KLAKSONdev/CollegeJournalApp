USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveLessonNote
    @GroupId    INT,
    @SubjectId  INT,
    @LessonDate DATE,
    @NoteText   NVARCHAR(300),
    @UserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @cleaned NVARCHAR(300) = LTRIM(RTRIM(ISNULL(@NoteText, N'')));

    IF LEN(@cleaned) = 0
    BEGIN
        -- Удаляем запись если текст пустой
        DELETE FROM dbo.LessonNotes
        WHERE GroupId = @GroupId AND SubjectId = @SubjectId AND LessonDate = @LessonDate;
    END
    ELSE
    BEGIN
        -- UPSERT
        MERGE dbo.LessonNotes AS t
        USING (SELECT @GroupId AS GroupId, @SubjectId AS SubjectId, @LessonDate AS LessonDate) AS s
        ON t.GroupId = s.GroupId AND t.SubjectId = s.SubjectId AND t.LessonDate = s.LessonDate
        WHEN MATCHED THEN
            UPDATE SET NoteText = @cleaned, UpdatedAt = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (GroupId, SubjectId, LessonDate, NoteText, CreatedByUserId, CreatedAt)
            VALUES (@GroupId, @SubjectId, @LessonDate, @cleaned, @UserId, GETDATE());
    END
END;
GO
