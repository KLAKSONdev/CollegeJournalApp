USE CollegeJournal;
GO

-- Возвращает всех активных преподавателей.
-- Используется для фильтра на странице расписания и в других списках.
CREATE OR ALTER PROCEDURE [dbo].[sp_GetAllTeachers]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.TeacherId,
        t.LastName + ' ' + t.FirstName +
            CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != ''
                 THEN ' ' + LTRIM(RTRIM(t.MiddleName))
                 ELSE ''
            END AS FullName
    FROM dbo.Teachers t
    WHERE t.IsDeleted = 0
    ORDER BY t.LastName, t.FirstName;
END
GO
