USE CollegeJournal;
GO

-- ============================================================
-- cleanup_seed.sql — полная очистка тестовых данных
-- ============================================================

BEGIN TRY
    BEGIN TRANSACTION;

    -- -------------------------------------------------------
    -- Шаг 0: диагностика — покажет какие логины нашли и их ID
    -- -------------------------------------------------------
    SELECT UserId, Login FROM dbo.Users
    WHERE UserId IN (1,2,3,4,5,6,7,8,9,10,11)
       OR Login IN (N'admin',N'ivanova_m',N'petrov_a',
                    N'smirnov_d',N'morozov_n',
                    N'kozlova_a',N'novikov_ar',N'lebedeva_o',
                    N'zaitseva_d',N'orlov_m',N'sokolova_v');

    -- -------------------------------------------------------
    -- Собираем ВСЕ UserId наших пользователей (по ID и логину)
    -- -------------------------------------------------------
    DECLARE @U TABLE (UserId INT PRIMARY KEY);
    INSERT INTO @U
    SELECT DISTINCT UserId FROM dbo.Users
    WHERE UserId IN (1,2,3,4,5,6,7,8,9,10,11)
       OR Login IN (N'admin',N'ivanova_m',N'petrov_a',
                    N'smirnov_d',N'morozov_n',
                    N'kozlova_a',N'novikov_ar',N'lebedeva_o',
                    N'zaitseva_d',N'orlov_m',N'sokolova_v');

    -- StudentId привязанные к нашим пользователям
    DECLARE @S TABLE (StudentId INT PRIMARY KEY);
    INSERT INTO @S
    SELECT DISTINCT StudentId FROM dbo.Students
    WHERE StudentId IN (1,2,3,4,5,6,7,8)
       OR UserId IN (SELECT UserId FROM @U);

    -- ScheduleId для групп 1 и 2
    DECLARE @Sch TABLE (ScheduleId INT PRIMARY KEY);
    INSERT INTO @Sch
    SELECT DISTINCT ScheduleId FROM dbo.Schedule
    WHERE ScheduleId IN (1,2,3,4,5,6,7,8,9,10,11,12)
       OR GroupId IN (1,2);

    -- SubjectId для групп 1 и 2
    DECLARE @Sub TABLE (SubjectId INT PRIMARY KEY);
    INSERT INTO @Sub
    SELECT DISTINCT SubjectId FROM dbo.Subjects
    WHERE SubjectId IN (1,2,3,4,5,6,7,8)
       OR GroupId IN (1,2);

    -- -------------------------------------------------------
    -- 1. Attendance
    --    FK: StudentId, ScheduleId, MarkedById
    -- -------------------------------------------------------
    DELETE FROM dbo.Attendance
    WHERE StudentId  IN (SELECT StudentId  FROM @S)
       OR ScheduleId IN (SELECT ScheduleId FROM @Sch)
       OR MarkedById IN (SELECT UserId FROM @U);

    -- -------------------------------------------------------
    -- 2. Grades
    --    FK: StudentId, SubjectId, AddedById
    -- -------------------------------------------------------
    DELETE FROM dbo.Grades
    WHERE StudentId IN (SELECT StudentId FROM @S)
       OR SubjectId IN (SELECT SubjectId FROM @Sub)
       OR AddedById IN (SELECT UserId FROM @U);

    -- -------------------------------------------------------
    -- 3. Achievements
    --    FK: StudentId, AddedById
    -- -------------------------------------------------------
    DELETE FROM dbo.Achievements
    WHERE StudentId IN (SELECT StudentId FROM @S)
       OR AddedById IN (SELECT UserId FROM @U);

    -- -------------------------------------------------------
    -- 4. StudentSocialInfo
    -- -------------------------------------------------------
    DELETE FROM dbo.StudentSocialInfo
    WHERE StudentId IN (SELECT StudentId FROM @S);

    -- -------------------------------------------------------
    -- 5. Parents
    -- -------------------------------------------------------
    DELETE FROM dbo.Parents
    WHERE StudentId IN (SELECT StudentId FROM @S);

    -- -------------------------------------------------------
    -- 6. Documents
    --    FK: GroupId, UploadedById
    -- -------------------------------------------------------
    DELETE FROM dbo.Documents
    WHERE GroupId      IN (1,2)
       OR UploadedById IN (SELECT UserId FROM @U);

    -- -------------------------------------------------------
    -- 7. Schedule
    -- -------------------------------------------------------
    DELETE FROM dbo.Schedule
    WHERE ScheduleId IN (SELECT ScheduleId FROM @Sch);

    -- -------------------------------------------------------
    -- 8. Students — сначала снимаем CHECK на общежитие
    -- -------------------------------------------------------
    UPDATE dbo.Students
       SET DormitoryId = NULL, RoomNumber = NULL
    WHERE StudentId IN (SELECT StudentId FROM @S);

    DELETE FROM dbo.Students
    WHERE StudentId IN (SELECT StudentId FROM @S);

    -- -------------------------------------------------------
    -- 9. Subjects
    -- -------------------------------------------------------
    DELETE FROM dbo.Subjects
    WHERE SubjectId IN (SELECT SubjectId FROM @Sub);

    -- -------------------------------------------------------
    -- 10. Groups — обнуляем CuratorId перед удалением
    -- -------------------------------------------------------
    UPDATE dbo.Groups SET CuratorId = NULL
    WHERE GroupId IN (1,2)
       OR CuratorId IN (SELECT UserId FROM @U);

    DELETE FROM dbo.Groups WHERE GroupId IN (1,2);

    -- -------------------------------------------------------
    -- 11. Dormitories
    -- -------------------------------------------------------
    DELETE FROM dbo.Dormitories WHERE DormitoryId IN (1,2);

    -- -------------------------------------------------------
    -- 12. Users — по собранным ID (включают нестандартные UserId)
    -- -------------------------------------------------------
    DELETE FROM dbo.Users WHERE UserId IN (SELECT UserId FROM @U);

    COMMIT TRANSACTION;
    PRINT N'Готово. Теперь запустите seed_data.sql.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT N'ОШИБКА #' + CAST(ERROR_NUMBER() AS NVARCHAR) +
          N' стр.' + CAST(ERROR_LINE() AS NVARCHAR) +
          N': ' + ERROR_MESSAGE();
END CATCH;
GO
