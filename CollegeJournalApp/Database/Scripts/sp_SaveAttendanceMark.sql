USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- INSERT, UPDATE или восстановление записи посещаемости.
-- 1. Есть активная запись (IsDeleted=0)  → UPDATE
-- 2. Есть мягко удалённая (IsDeleted=1)  → восстановить + UPDATE
-- 3. Нет записи вообще                   → INSERT
CREATE OR ALTER PROCEDURE dbo.sp_SaveAttendanceMark
    @MarkedById INT,
    @StudentId  INT,
    @ScheduleId INT,
    @LessonDate DATE,
    @Status     NVARCHAR(50),
    @Reason     NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Активная запись существует → просто обновляем
    IF EXISTS (
        SELECT 1 FROM dbo.Attendance
        WHERE StudentId  = @StudentId
          AND ScheduleId = @ScheduleId
          AND LessonDate = @LessonDate
          AND IsDeleted  = 0
    )
    BEGIN
        UPDATE dbo.Attendance
        SET    Status     = @Status,
               Reason     = NULLIF(@Reason, N''),
               MarkedById = @MarkedById
        WHERE  StudentId  = @StudentId
          AND  ScheduleId = @ScheduleId
          AND  LessonDate = @LessonDate
          AND  IsDeleted  = 0;
        RETURN;
    END

    -- Запись есть но мягко удалена → восстанавливаем с новыми данными
    IF EXISTS (
        SELECT 1 FROM dbo.Attendance
        WHERE StudentId  = @StudentId
          AND ScheduleId = @ScheduleId
          AND LessonDate = @LessonDate
          AND IsDeleted  = 1
    )
    BEGIN
        UPDATE dbo.Attendance
        SET    Status     = @Status,
               Reason     = NULLIF(@Reason, N''),
               MarkedById = @MarkedById,
               IsDeleted  = 0
        WHERE  StudentId  = @StudentId
          AND  ScheduleId = @ScheduleId
          AND  LessonDate = @LessonDate
          AND  IsDeleted  = 1;
        RETURN;
    END

    -- Записи нет вообще → вставляем новую
    INSERT INTO dbo.Attendance
        (StudentId, ScheduleId, MarkedById, LessonDate, Status, Reason, IsDeleted)
    VALUES
        (@StudentId, @ScheduleId, @MarkedById, @LessonDate,
         @Status, NULLIF(@Reason, N''), 0);
END;
GO
