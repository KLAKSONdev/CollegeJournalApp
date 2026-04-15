USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_SaveScheduleItem', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SaveScheduleItem;
GO

CREATE PROCEDURE [dbo].[sp_SaveScheduleItem]
    @ScheduleId   INT          = NULL,
    @GroupId      INT,
    @DayOfWeek    TINYINT,
    @LessonNumber TINYINT,
    @SubjectId    INT,
    @Classroom    NVARCHAR(50) = NULL,
    @WeekType     NVARCHAR(20) = N'Обе'
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Слот группы уже занят?
    IF EXISTS (
        SELECT 1 FROM Schedule
        WHERE GroupId      = @GroupId
          AND DayOfWeek    = @DayOfWeek
          AND LessonNumber = @LessonNumber
          AND IsDeleted    = 0
          AND (@ScheduleId IS NULL OR ScheduleId <> @ScheduleId)
    )
    BEGIN
        DECLARE @GName NVARCHAR(100);
        SELECT @GName = GroupName FROM Groups WHERE GroupId = @GroupId;
        DECLARE @E1 NVARCHAR(400);
        SET @E1 = N'У группы ' + @GName + N' уже есть пара №' + CAST(@LessonNumber AS NVARCHAR) + N' в этот день.';
        RAISERROR(@E1, 16, 1);
        RETURN;
    END

    -- 2. Преподаватель уже занят в другой группе?
    DECLARE @TeacherId INT;
    SELECT @TeacherId = TeacherId FROM Subjects
    WHERE SubjectId = @SubjectId AND IsDeleted = 0;

    IF @TeacherId IS NOT NULL
    BEGIN
        DECLARE @BusyGroup NVARCHAR(100);
        SELECT TOP 1 @BusyGroup = g.GroupName
        FROM Schedule s
        INNER JOIN Subjects sub ON sub.SubjectId = s.SubjectId
        INNER JOIN Groups   g   ON g.GroupId     = s.GroupId
        WHERE sub.TeacherId  = @TeacherId
          AND s.DayOfWeek    = @DayOfWeek
          AND s.LessonNumber = @LessonNumber
          AND s.IsDeleted    = 0
          AND (@ScheduleId IS NULL OR s.ScheduleId <> @ScheduleId);

        IF @BusyGroup IS NOT NULL
        BEGIN
            DECLARE @TName NVARCHAR(300);
            SELECT @TName = LastName + ' ' + FirstName
            FROM Teachers WHERE TeacherId = @TeacherId;
            DECLARE @E2 NVARCHAR(400);
            SET @E2 = N'Преподаватель ' + @TName + N' уже занят в это время (группа ' + @BusyGroup + N').';
            RAISERROR(@E2, 16, 1);
            RETURN;
        END
    END

    -- Определяем время пары по номеру
    DECLARE @StartTime TIME, @EndTime TIME;
    SELECT
        @StartTime = CASE @LessonNumber
            WHEN 1 THEN '08:30' WHEN 2 THEN '10:15' WHEN 3 THEN '12:30'
            WHEN 4 THEN '14:15' WHEN 5 THEN '16:00' WHEN 6 THEN '17:45'
            ELSE NULL END,
        @EndTime = CASE @LessonNumber
            WHEN 1 THEN '10:05' WHEN 2 THEN '11:50' WHEN 3 THEN '14:05'
            WHEN 4 THEN '15:50' WHEN 5 THEN '17:35' WHEN 6 THEN '19:20'
            ELSE NULL END;

    IF @ScheduleId IS NULL
    BEGIN
        INSERT INTO Schedule
            (GroupId, DayOfWeek, LessonNumber, SubjectId, Classroom, WeekType, StartTime, EndTime, IsDeleted)
        VALUES
            (@GroupId, @DayOfWeek, @LessonNumber, @SubjectId, @Classroom, @WeekType, @StartTime, @EndTime, 0);
        SELECT SCOPE_IDENTITY() AS ScheduleId;
    END
    ELSE
    BEGIN
        UPDATE Schedule
        SET GroupId      = @GroupId,
            DayOfWeek    = @DayOfWeek,
            LessonNumber = @LessonNumber,
            SubjectId    = @SubjectId,
            Classroom    = @Classroom,
            WeekType     = @WeekType,
            StartTime    = @StartTime,
            EndTime      = @EndTime
        WHERE ScheduleId = @ScheduleId;
        SELECT @ScheduleId AS ScheduleId;
    END
END
GO
