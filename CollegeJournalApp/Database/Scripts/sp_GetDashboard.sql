USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDashboard
    @UserId   INT,
    @RoleName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- ═══════════════════════════════════════════════════════════════
    --  ADMIN  — системная статистика
    -- ═══════════════════════════════════════════════════════════════
    IF @RoleName = N'Admin'
    BEGIN
        SELECT
            (SELECT COUNT(*) FROM dbo.Users    WHERE IsDeleted = 0)                             AS UserCount,
            (SELECT COUNT(*) FROM dbo.Students WHERE IsDeleted = 0)                             AS StudentCount,
            (SELECT COUNT(*) FROM dbo.Groups   WHERE IsDeleted = 0 AND IsGraduated = 0)         AS GroupCount,
            (SELECT COUNT(*) FROM dbo.Teachers WHERE IsDeleted = 0 AND IsActive = 1)            AS TeacherCount,
            ISNULL(
                (SELECT AVG(CAST(GradeValue AS DECIMAL(4,2)))
                 FROM dbo.Grades WHERE IsDeleted = 0), 0)                                       AS AvgGrade,
            (SELECT COUNT(*) FROM dbo.AuditLog
             WHERE Action = N'LOGIN'
               AND CAST(ActionAt AS DATE) = CAST(GETDATE() AS DATE))                            AS LoginsToday,
            (SELECT COUNT(*) FROM dbo.AuditLog
             WHERE CAST(ActionAt AS DATE) = CAST(GETDATE() AS DATE))                            AS ActionsToday,
            (SELECT COUNT(*) FROM dbo.Users
             WHERE IsDeleted = 0
               AND MONTH(CreatedAt) = MONTH(GETDATE())
               AND YEAR(CreatedAt)  = YEAR(GETDATE()))                                          AS NewUsersThisMonth,
            N'Система'                                                                           AS GroupName,
            NULL                                                                                 AS GroupId,
            NULL                                                                                 AS AttendancePercent,
            NULL                                                                                 AS AbsentCount,
            NULL                                                                                 AS LessonsToday,
            NULL                                                                                 AS AchievCount;
        RETURN;
    END

    -- ═══════════════════════════════════════════════════════════════
    --  CURATOR  — статистика по своей группе
    -- ═══════════════════════════════════════════════════════════════
    IF @RoleName = N'Curator'
    BEGIN
        DECLARE @CurGroupId INT;
        SELECT TOP 1 @CurGroupId = GroupId
        FROM dbo.Groups
        WHERE CuratorId = @UserId AND IsDeleted = 0;

        DECLARE @CurTotal INT =
            (SELECT COUNT(*) FROM dbo.Attendance a
             INNER JOIN dbo.Students st ON st.StudentId = a.StudentId
             WHERE st.GroupId   = @CurGroupId AND st.IsDeleted = 0
               AND a.IsDeleted  = 0
               AND CAST(a.LessonDate AS DATE) = CAST(GETDATE() AS DATE));

        DECLARE @CurPresent INT =
            (SELECT COUNT(*) FROM dbo.Attendance a
             INNER JOIN dbo.Students st ON st.StudentId = a.StudentId
             WHERE st.GroupId   = @CurGroupId AND st.IsDeleted = 0
               AND a.IsDeleted  = 0 AND a.Status = N'Присутствовал'
               AND CAST(a.LessonDate AS DATE) = CAST(GETDATE() AS DATE));

        SELECT
            NULL                                                                                    AS UserCount,
            (SELECT COUNT(*) FROM dbo.Students
             WHERE GroupId = @CurGroupId AND IsDeleted = 0)                                        AS StudentCount,
            NULL                                                                                    AS GroupCount,
            ISNULL(
                (SELECT AVG(CAST(gr.GradeValue AS DECIMAL(4,2)))
                 FROM dbo.Grades gr
                 INNER JOIN dbo.Students st ON st.StudentId = gr.StudentId
                 WHERE st.GroupId = @CurGroupId AND st.IsDeleted = 0
                   AND gr.IsDeleted = 0), 0)                                                       AS AvgGrade,
            NULL                                                                                    AS LoginsToday,
            NULL                                                                                    AS ActionsToday,
            ISNULL((SELECT TOP 1 GroupName FROM dbo.Groups
                    WHERE GroupId = @CurGroupId), N'—')                                            AS GroupName,
            @CurGroupId                                                                             AS GroupId,
            CASE WHEN @CurTotal > 0
                 THEN CAST(CAST(100.0 * @CurPresent / @CurTotal AS DECIMAL(5,1)) AS NVARCHAR(20))
                 ELSE NULL END                                                                      AS AttendancePercent,
            (SELECT COUNT(*) FROM dbo.Attendance a
             INNER JOIN dbo.Students st ON st.StudentId = a.StudentId
             WHERE st.GroupId  = @CurGroupId AND st.IsDeleted = 0
               AND a.IsDeleted = 0 AND a.Status = N'Отсутствовал'
               AND a.LessonDate >= DATEADD(DAY, -30, GETDATE()))                                   AS AbsentCount,
            NULL                                                                                    AS LessonsToday,
            NULL                                                                                    AS AchievCount;
        RETURN;
    END

    -- ═══════════════════════════════════════════════════════════════
    --  HEADMAN  — статистика группы: сделан упор на сегодняшний день
    -- ═══════════════════════════════════════════════════════════════
    IF @RoleName = N'Headman'
    BEGIN
        DECLARE @HmGroupId INT;
        SELECT TOP 1 @HmGroupId = GroupId
        FROM dbo.Students WHERE UserId = @UserId AND IsDeleted = 0;

        DECLARE @HmTodayDow TINYINT =
            CASE WHEN DATEPART(dw, GETDATE()) = 1 THEN 7
                 ELSE CAST(DATEPART(dw, GETDATE()) - 1 AS TINYINT) END;

        SELECT
            NULL                                                                                    AS UserCount,
            (SELECT COUNT(*) FROM dbo.Students
             WHERE GroupId = @HmGroupId AND IsDeleted = 0)                                        AS StudentCount,
            NULL                                                                                    AS GroupCount,
            ISNULL(
                (SELECT AVG(CAST(gr.GradeValue AS DECIMAL(4,2)))
                 FROM dbo.Grades gr
                 INNER JOIN dbo.Students st ON st.StudentId = gr.StudentId
                 WHERE st.GroupId = @HmGroupId AND st.IsDeleted = 0
                   AND gr.IsDeleted = 0), 0)                                                       AS AvgGrade,
            NULL                                                                                    AS LoginsToday,
            NULL                                                                                    AS ActionsToday,
            ISNULL((SELECT TOP 1 GroupName FROM dbo.Groups
                    WHERE GroupId = @HmGroupId), N'—')                                            AS GroupName,
            @HmGroupId                                                                             AS GroupId,
            -- AttendancePercent: кол-во присутствующих сегодня (переиспользуем колонку)
            CAST(
                (SELECT COUNT(*) FROM dbo.Attendance a
                 INNER JOIN dbo.Students st ON st.StudentId = a.StudentId
                 WHERE st.GroupId   = @HmGroupId AND st.IsDeleted = 0
                   AND a.IsDeleted  = 0 AND a.Status = N'Присутствовал'
                   AND CAST(a.LessonDate AS DATE) = CAST(GETDATE() AS DATE))
            AS NVARCHAR(20))                                                                       AS AttendancePercent,
            (SELECT COUNT(*) FROM dbo.Attendance a
             INNER JOIN dbo.Students st ON st.StudentId = a.StudentId
             WHERE st.GroupId   = @HmGroupId AND st.IsDeleted = 0
               AND a.IsDeleted  = 0 AND a.Status = N'Отсутствовал'
               AND CAST(a.LessonDate AS DATE) = CAST(GETDATE() AS DATE))                          AS AbsentCount,
            (SELECT COUNT(*) FROM dbo.Schedule
             WHERE GroupId = @HmGroupId AND DayOfWeek = @HmTodayDow
               AND IsDeleted = 0)                                                                  AS LessonsToday,
            NULL                                                                                    AS AchievCount;
        RETURN;
    END

    -- ═══════════════════════════════════════════════════════════════
    --  STUDENT  — личная статистика
    -- ═══════════════════════════════════════════════════════════════
    BEGIN
        DECLARE @StStudentId INT, @StGroupId INT;
        SELECT @StStudentId = StudentId, @StGroupId = GroupId
        FROM dbo.Students WHERE UserId = @UserId AND IsDeleted = 0;

        DECLARE @StTodayDow TINYINT =
            CASE WHEN DATEPART(dw, GETDATE()) = 1 THEN 7
                 ELSE CAST(DATEPART(dw, GETDATE()) - 1 AS TINYINT) END;

        DECLARE @StAttTotal INT =
            (SELECT COUNT(*) FROM dbo.Attendance
             WHERE StudentId = @StStudentId AND IsDeleted = 0
               AND LessonDate >= DATEADD(DAY, -30, GETDATE()));

        DECLARE @StAttPresent INT =
            (SELECT COUNT(*) FROM dbo.Attendance
             WHERE StudentId = @StStudentId AND IsDeleted = 0
               AND Status = N'Присутствовал'
               AND LessonDate >= DATEADD(DAY, -30, GETDATE()));

        SELECT
            NULL                                                                                    AS UserCount,
            NULL                                                                                    AS StudentCount,
            NULL                                                                                    AS GroupCount,
            ISNULL(
                (SELECT AVG(CAST(GradeValue AS DECIMAL(4,2)))
                 FROM dbo.Grades
                 WHERE StudentId = @StStudentId AND IsDeleted = 0), 0)                            AS AvgGrade,
            NULL                                                                                    AS LoginsToday,
            NULL                                                                                    AS ActionsToday,
            ISNULL((SELECT TOP 1 GroupName FROM dbo.Groups
                    WHERE GroupId = @StGroupId), N'—')                                            AS GroupName,
            @StGroupId                                                                             AS GroupId,
            -- AttendancePercent: личный % посещаемости за 30 дней
            CASE WHEN @StAttTotal > 0
                 THEN CAST(CAST(100.0 * @StAttPresent / @StAttTotal AS DECIMAL(5,1)) AS NVARCHAR(20))
                 ELSE NULL END                                                                      AS AttendancePercent,
            (SELECT COUNT(*) FROM dbo.Attendance
             WHERE StudentId = @StStudentId AND IsDeleted = 0
               AND Status = N'Отсутствовал'
               AND LessonDate >= DATEADD(DAY, -30, GETDATE()))                                    AS AbsentCount,
            (SELECT COUNT(*) FROM dbo.Schedule
             WHERE GroupId = @StGroupId AND DayOfWeek = @StTodayDow
               AND IsDeleted = 0)                                                                  AS LessonsToday,
            (SELECT COUNT(*) FROM dbo.Achievements
             WHERE StudentId = @StStudentId AND IsDeleted = 0)                                    AS AchievCount;
    END
END;
GO
