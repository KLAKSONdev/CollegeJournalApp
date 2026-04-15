-- Возвращает всех преподавателей, у которых есть занятия в расписании.
-- Используется для фильтра администратора на странице расписания.

CREATE OR ALTER PROCEDURE [dbo].[sp_GetAllTeachers]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        t.TeacherId,
        t.LastName + ' ' + t.FirstName +
            CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != ''
                 THEN ' ' + LTRIM(RTRIM(t.MiddleName))
                 ELSE ''
            END AS FullName
    FROM Teachers t
    INNER JOIN Subjects sub ON sub.TeacherId = t.TeacherId
    INNER JOIN Schedule s   ON s.SubjectId  = sub.SubjectId
    WHERE t.IsDeleted = 0
      AND s.IsDeleted = 0
    ORDER BY 2;  -- сортировка по FullName
END
