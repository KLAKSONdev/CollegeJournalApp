-- =============================================
-- Хранимая процедура для импорта записи расписания из Excel
-- =============================================
CREATE OR ALTER PROCEDURE sp_ImportScheduleItem
    @DayOfWeek INT,
    @LessonNumber NVARCHAR(10) = NULL,
    @StartTime NVARCHAR(20) = NULL,
    @EndTime NVARCHAR(20) = NULL,
    @SubjectName NVARCHAR(255) = NULL,
    @Classroom NVARCHAR(50) = NULL,
    @TeacherName NVARCHAR(255) = NULL,
    @WeekType NVARCHAR(20) = NULL,
    @UserId INT,
    @RoleName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Получаем GroupId текущего пользователя
        DECLARE @GroupId INT;
        
        IF @RoleName = 'Староста' OR @RoleName = 'Студент'
        BEGIN
            SELECT @GroupId = GroupId 
            FROM Students 
            WHERE UserId = @UserId;
        END
        ELSE IF @RoleName = 'Куратор'
        BEGIN
            SELECT @GroupId = Id 
            FROM Groups 
            WHERE CuratorId = (SELECT Id FROM Users WHERE Id = @UserId);
        END
        ELSE IF @RoleName IN ('Администратор', 'Преподаватель')
        BEGIN
            -- Для администратора и преподавателя берём первую группу или NULL
            SELECT TOP 1 @GroupId = Id FROM Groups;
        END

        -- Если группа не найдена, создаём запись без привязки к группе (общее расписание)
        IF @GroupId IS NULL
        BEGIN
            -- Проверяем, существует ли уже такая запись
            IF NOT EXISTS (
                SELECT 1 FROM Schedule
                WHERE DayOfWeek = @DayOfWeek
                  AND LessonNumber = @LessonNumber
                  AND ISNULL(StartTime, '') = ISNULL(@StartTime, '')
                  AND SubjectId IS NULL
                  AND Classroom = @Classroom
            )
            BEGIN
                -- Находим или создаём предмет
                DECLARE @SubjectId INT;
                IF @SubjectName IS NOT NULL AND @SubjectName <> ''
                BEGIN
                    SELECT @SubjectId = Id FROM Subjects WHERE Name = @SubjectName;
                    
                    IF @SubjectId IS NULL
                    BEGIN
                        INSERT INTO Subjects (Name) VALUES (@SubjectName);
                        SET @SubjectId = SCOPE_IDENTITY();
                    END
                END

                -- Находим или создаём преподавателя
                DECLARE @TeacherId INT;
                IF @TeacherName IS NOT NULL AND @TeacherName <> ''
                BEGIN
                    SELECT @TeacherId = Id FROM Teachers WHERE FullName = @TeacherName;
                    
                    IF @TeacherId IS NULL
                    BEGIN
                        INSERT INTO Teachers (FullName) VALUES (@TeacherName);
                        SET @TeacherId = SCOPE_IDENTITY();
                    END
                END

                -- Вставляем запись в расписание
                INSERT INTO Schedule (
                    DayOfWeek, LessonNumber, StartTime, EndTime,
                    SubjectId, Classroom, TeacherId, WeekType,
                    CreatedAt, UpdatedAt
                ) VALUES (
                    @DayOfWeek, @LessonNumber, @StartTime, @EndTime,
                    @SubjectId, @Classroom, @TeacherId, @WeekType,
                    GETDATE(), GETDATE()
                );
            END
        END
        ELSE
        BEGIN
            -- Для группы проверяем существование записи
            IF NOT EXISTS (
                SELECT 1 FROM Schedule
                WHERE GroupId = @GroupId
                  AND DayOfWeek = @DayOfWeek
                  AND LessonNumber = @LessonNumber
            )
            BEGIN
                -- Находим или создаём предмет
                DECLARE @SubjId INT;
                IF @SubjectName IS NOT NULL AND @SubjectName <> ''
                BEGIN
                    SELECT @SubjId = Id FROM Subjects WHERE Name = @SubjectName;
                    
                    IF @SubjId IS NULL
                    BEGIN
                        INSERT INTO Subjects (Name) VALUES (@SubjectName);
                        SET @SubjId = SCOPE_IDENTITY();
                    END
                END

                -- Находим или создаём преподавателя
                DECLARE @TeachId INT;
                IF @TeacherName IS NOT NULL AND @TeacherName <> ''
                BEGIN
                    SELECT @TeachId = Id FROM Teachers WHERE FullName = @TeacherName;
                    
                    IF @TeachId IS NULL
                    BEGIN
                        INSERT INTO Teachers (FullName) VALUES (@TeacherName);
                        SET @TeachId = SCOPE_IDENTITY();
                    END
                END

                -- Вставляем запись в расписание для группы
                INSERT INTO Schedule (
                    GroupId, DayOfWeek, LessonNumber, StartTime, EndTime,
                    SubjectId, Classroom, TeacherId, WeekType,
                    CreatedAt, UpdatedAt
                ) VALUES (
                    @GroupId, @DayOfWeek, @LessonNumber, @StartTime, @EndTime,
                    @SubjId, @Classroom, @TeachId, @WeekType,
                    GETDATE(), GETDATE()
                );
            END
        END
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- =============================================
-- Пример использования:
-- EXEC sp_ImportScheduleItem 
--     @DayOfWeek = 1,
--     @LessonNumber = '1',
--     @StartTime = '08:30',
--     @EndTime = '10:00',
--     @SubjectName = 'Математика',
--     @Classroom = '101',
--     @TeacherName = 'Петров П.П.',
--     @WeekType = 'Обе',
--     @UserId = 1,
--     @RoleName = 'Староста';
-- =============================================
