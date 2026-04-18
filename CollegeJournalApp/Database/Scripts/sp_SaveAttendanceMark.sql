USE CollegeJournal;
GO

-- INSERT или UPDATE записи посещаемости.
-- Если запись с таким StudentId+ScheduleId+LessonDate уже есть — обновляет.
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
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Attendance
            (StudentId, ScheduleId, MarkedById, LessonDate, Status, Reason, IsDeleted)
        VALUES
            (@StudentId, @ScheduleId, @MarkedById, @LessonDate,
             @Status, NULLIF(@Reason, N''), 0);
    END
END;
GO
