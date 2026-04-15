USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_ImportScheduleItem', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ImportScheduleItem;
GO

CREATE PROCEDURE [dbo].[sp_ImportScheduleItem]
    @GroupName    NVARCHAR(100),
    @DayOfWeek    TINYINT,
    @LessonNumber TINYINT,
    @SubjectName  NVARCHAR(255),
    @Classroom    NVARCHAR(50)  = NULL,
    @WeekType     NVARCHAR(20)  = N'Обе',
    @TeacherName  NVARCHAR(300) = NULL   -- Фамилия И.О. или «Фамилия Имя», необязательно
AS
BEGIN
    SET NOCOUNT ON;

    -- ── 1. Найти группу ───────────────────────────────────────────────────────
    DECLARE @GroupId INT;
    SELECT @GroupId = GroupId FROM Groups
    WHERE GroupName = @GroupName AND IsDeleted = 0;

    IF @GroupId IS NULL
    BEGIN
        DECLARE @E1 NVARCHAR(300);
        SET @E1 = N'Группа "' + @GroupName + N'" не найдена в базе данных.';
        RAISERROR(@E1, 16, 1);
        RETURN;
    END

    -- ── 2. Найти или создать предмет ─────────────────────────────────────────
    DECLARE @SubjectId INT;
    SELECT TOP 1 @SubjectId = SubjectId FROM Subjects
    WHERE SubjectName = @SubjectName AND GroupId = @GroupId AND IsDeleted = 0;

    IF @SubjectId IS NULL
    BEGIN
        -- Найти преподавателя по имени (если указан)
        DECLARE @TeacherId INT = NULL;
        IF @TeacherName IS NOT NULL AND LEN(LTRIM(RTRIM(@TeacherName))) > 0
        BEGIN
            -- Пробуем точное совпадение «Фамилия Имя» или «Фамилия Имя Отчество»
            SELECT TOP 1 @TeacherId = TeacherId
            FROM Teachers
            WHERE IsDeleted = 0
              AND (
                    LastName + ' ' + FirstName = @TeacherName
                 OR LastName + ' ' + FirstName + ' ' + ISNULL(MiddleName,'') = @TeacherName
                 OR LastName = @TeacherName  -- только фамилия
              )
            ORDER BY TeacherId;

            -- Если не нашли точно — ищем по фамилии (первое слово)
            IF @TeacherId IS NULL
            BEGIN
                DECLARE @LastNamePart NVARCHAR(100);
                SET @LastNamePart = LEFT(@TeacherName, CHARINDEX(' ', @TeacherName + ' ') - 1);
                SELECT TOP 1 @TeacherId = TeacherId
                FROM Teachers
                WHERE IsDeleted = 0 AND LastName = @LastNamePart
                ORDER BY TeacherId;
            END
        END

        -- Создаём предмет
        INSERT INTO Subjects (SubjectName, GroupId, TeacherId, IsDeleted)
        VALUES (@SubjectName, @GroupId, @TeacherId, 0);

        SET @SubjectId = SCOPE_IDENTITY();
    END

    -- ── 3. Проверка занятости преподавателя ──────────────────────────────────
    DECLARE @TeacherIdCheck INT;
    SELECT @TeacherIdCheck = TeacherId FROM Subjects WHERE SubjectId = @SubjectId;

    IF @TeacherIdCheck IS NOT NULL
    BEGIN
        DECLARE @BusyGroup NVARCHAR(100);
        SELECT TOP 1 @BusyGroup = g.GroupName
        FROM Schedule s
        INNER JOIN Subjects sub ON sub.SubjectId = s.SubjectId
        INNER JOIN Groups   g   ON g.GroupId     = s.GroupId
        WHERE sub.TeacherId  = @TeacherIdCheck
          AND s.DayOfWeek    = @DayOfWeek
          AND s.LessonNumber = @LessonNumber
          AND s.IsDeleted    = 0
          AND s.GroupId     <> @GroupId;

        IF @BusyGroup IS NOT NULL
        BEGIN
            DECLARE @TName NVARCHAR(300);
            SELECT @TName = LastName + ' ' + FirstName FROM Teachers WHERE TeacherId = @TeacherIdCheck;
            DECLARE @E3 NVARCHAR(400);
            SET @E3 = N'Преподаватель ' + @TName + N' уже занят в это время (группа ' + @BusyGroup + N').';
            RAISERROR(@E3, 16, 1);
            RETURN;
        END
    END

    -- ── 4. Время пары ─────────────────────────────────────────────────────────
    DECLARE @StartTime TIME, @EndTime TIME;
    SELECT
        @StartTime = CASE @LessonNumber
            WHEN 1 THEN '08:30' WHEN 2 THEN '10:15' WHEN 3 THEN '12:30'
            WHEN 4 THEN '14:15' WHEN 5 THEN '16:00' WHEN 6 THEN '17:45' ELSE NULL END,
        @EndTime = CASE @LessonNumber
            WHEN 1 THEN '10:05' WHEN 2 THEN '11:50' WHEN 3 THEN '14:05'
            WHEN 4 THEN '15:50' WHEN 5 THEN '17:35' WHEN 6 THEN '19:20' ELSE NULL END;

    -- ── 5. Обновить или вставить ──────────────────────────────────────────────
    IF EXISTS (
        SELECT 1 FROM Schedule
        WHERE GroupId = @GroupId AND DayOfWeek = @DayOfWeek
          AND LessonNumber = @LessonNumber AND IsDeleted = 0
    )
    BEGIN
        UPDATE Schedule
        SET SubjectId  = @SubjectId,
            Classroom  = @Classroom,
            WeekType   = @WeekType,
            StartTime  = @StartTime,
            EndTime    = @EndTime
        WHERE GroupId = @GroupId AND DayOfWeek = @DayOfWeek
          AND LessonNumber = @LessonNumber AND IsDeleted = 0;
    END
    ELSE
    BEGIN
        INSERT INTO Schedule
            (GroupId, DayOfWeek, LessonNumber, SubjectId, Classroom, WeekType, StartTime, EndTime, IsDeleted)
        VALUES
            (@GroupId, @DayOfWeek, @LessonNumber, @SubjectId, @Classroom, @WeekType, @StartTime, @EndTime, 0);
    END
END
GO
