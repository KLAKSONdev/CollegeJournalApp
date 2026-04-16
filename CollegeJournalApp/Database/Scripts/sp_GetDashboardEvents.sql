USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardEvents
    @UserId   INT,
    @RoleName NVARCHAR(50),
    @Limit    INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    -- ═══════════════════════════════════════════════════════════════
    --  ADMIN  — последние записи журнала аудита
    -- ═══════════════════════════════════════════════════════════════
    IF @RoleName = N'Admin'
    BEGIN
        SELECT TOP (@Limit)
            v.LogId,
            v.ReadableAction    AS EventText,
            v.ActionAt          AS EventTime,
            v.Action            AS EventType
        FROM dbo.vw_AuditLog v
        ORDER BY v.ActionAt DESC;
        RETURN;
    END

    -- ═══════════════════════════════════════════════════════════════
    --  CURATOR  — последние оценки и пропуски по своей группе
    -- ═══════════════════════════════════════════════════════════════
    IF @RoleName = N'Curator'
    BEGIN
        DECLARE @CurGrpId INT;
        SELECT TOP 1 @CurGrpId = GroupId
        FROM dbo.Groups WHERE CuratorId = @UserId AND IsDeleted = 0;

        -- Последние оценки
        SELECT TOP (@Limit)
            NULL                                                                    AS LogId,
            u.LastName + N' ' + u.FirstName
                + N' — ' + sub.SubjectName
                + N': ' + CAST(gr.GradeValue AS NVARCHAR)                         AS EventText,
            CAST(gr.GradeDate AS DATETIME)                                         AS EventTime,
            N'GRADE'                                                               AS EventType
        FROM dbo.Grades gr
        INNER JOIN dbo.Students  st  ON st.StudentId  = gr.StudentId
        INNER JOIN dbo.Users     u   ON u.UserId      = st.UserId
        INNER JOIN dbo.Subjects  sub ON sub.SubjectId = gr.SubjectId
        WHERE st.GroupId = @CurGrpId AND st.IsDeleted = 0 AND gr.IsDeleted = 0
        ORDER BY gr.GradeDate DESC;
        RETURN;
    END

    -- ═══════════════════════════════════════════════════════════════
    --  HEADMAN  — расписание на сегодня + последние пропуски группы
    -- ═══════════════════════════════════════════════════════════════
    IF @RoleName = N'Headman'
    BEGIN
        DECLARE @HmGrpId INT;
        SELECT TOP 1 @HmGrpId = GroupId
        FROM dbo.Students WHERE UserId = @UserId AND IsDeleted = 0;

        DECLARE @TodayDow TINYINT =
            CASE WHEN DATEPART(dw, GETDATE()) = 1 THEN 7
                 ELSE CAST(DATEPART(dw, GETDATE()) - 1 AS TINYINT) END;

        -- Сегодняшнее расписание (как лента событий)
        SELECT
            NULL                                                                    AS LogId,
            CAST(sc.LessonNumber AS NVARCHAR) + N' пара  ·  '
                + sub.SubjectName
                + N'  ·  ' + ISNULL(sc.Classroom, N'—')                          AS EventText,
            CAST(GETDATE() AS DATETIME)                                            AS EventTime,
            N'SCHEDULE'                                                            AS EventType
        FROM dbo.Schedule sc
        INNER JOIN dbo.Subjects sub ON sub.SubjectId = sc.SubjectId
        WHERE sc.GroupId = @HmGrpId AND sc.DayOfWeek = @TodayDow AND sc.IsDeleted = 0
        ORDER BY sc.LessonNumber;

        RETURN;
    END

    -- ═══════════════════════════════════════════════════════════════
    --  STUDENT  — собственные последние оценки
    -- ═══════════════════════════════════════════════════════════════
    BEGIN
        DECLARE @StId INT;
        SELECT TOP 1 @StId = StudentId
        FROM dbo.Students WHERE UserId = @UserId AND IsDeleted = 0;

        SELECT TOP (@Limit)
            NULL                                                                    AS LogId,
            sub.SubjectName
                + N':  ' + CAST(gr.GradeValue AS NVARCHAR)
                + N'  (' + gr.GradeType + N')'                                    AS EventText,
            CAST(gr.GradeDate AS DATETIME)                                         AS EventTime,
            N'GRADE'                                                               AS EventType
        FROM dbo.Grades gr
        INNER JOIN dbo.Subjects sub ON sub.SubjectId = gr.SubjectId
        WHERE gr.StudentId = @StId AND gr.IsDeleted = 0
        ORDER BY gr.GradeDate DESC;
    END
END;
GO
