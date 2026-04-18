USE CollegeJournal;
GO

-- Преподаватели доступные для назначения куратором группы @GroupId.
-- Исключает тех, кто уже куратор ДРУГОЙ группы.
-- Текущий куратор данной группы всегда включается в список.
CREATE OR ALTER PROCEDURE dbo.sp_GetTeachersForCurator
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.TeacherId,
        t.UserId,
        t.LastName + N' ' + t.FirstName +
            CASE WHEN t.MiddleName IS NOT NULL AND LTRIM(RTRIM(t.MiddleName)) != N''
                 THEN N' ' + LTRIM(RTRIM(t.MiddleName)) ELSE N'' END AS FullName,
        r.RoleName
    FROM dbo.Teachers t
    INNER JOIN dbo.Users u ON u.UserId = t.UserId
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE t.IsDeleted  = 0
      AND t.UserId     IS NOT NULL
      AND u.IsDeleted  = 0
      -- Не куратор другой группы (куратор текущей группы — разрешён)
      AND NOT EXISTS (
            SELECT 1 FROM dbo.Groups g
            WHERE g.CuratorId = t.UserId
              AND g.GroupId  <> @GroupId
              AND g.IsDeleted = 0
      )
    ORDER BY t.LastName, t.FirstName;
END;
GO
