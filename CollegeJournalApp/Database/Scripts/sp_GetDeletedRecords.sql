USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- sp_GetDeletedRecords
-- Возвращает все мягко удалённые записи из ключевых таблиц
-- (Students, Documents, Parents) для страницы Корзины.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetDeletedRecords
    @TableFilter NVARCHAR(50) = NULL   -- NULL = все таблицы
AS
BEGIN
    SET NOCOUNT ON;

    -- Последнее DELETE-событие для каждой записи из AuditLog
    WITH LastDelete AS
    (
        SELECT
            al.TableName,
            al.RecordId,
            al.UserId      AS DeletedByUserId,
            al.ActionAt   AS DeletedAt,
            ROW_NUMBER() OVER
                (PARTITION BY al.TableName, al.RecordId
                 ORDER BY al.ActionAt DESC) AS rn
        FROM dbo.AuditLog al
        WHERE al.Action = N'DELETE'
    )

    SELECT
        src.TableName,
        src.RecordId,
        src.RecordName,
        src.ExtraInfo,
        src.GroupInfo,
        ld.DeletedAt,
        ISNULL(du.LastName + N' ' + du.FirstName, N'—') AS DeletedByName
    FROM
    (
        -- ── Студенты ──────────────────────────────────────────────────────
        SELECT
            N'Students'                                        AS TableName,
            s.StudentId                                        AS RecordId,
            u.LastName + N' ' + u.FirstName
                + ISNULL(N' ' + u.MiddleName, N'')            AS RecordName,
            ISNULL(s.StudentCode, N'—')                        AS ExtraInfo,
            g.GroupName                                        AS GroupInfo
        FROM dbo.Students s
        JOIN dbo.Users  u ON s.UserId  = u.UserId
        JOIN dbo.Groups g ON s.GroupId = g.GroupId
        WHERE s.IsDeleted = 1
          AND (@TableFilter IS NULL OR @TableFilter = N'Students')

        UNION ALL

        -- ── Документы ─────────────────────────────────────────────────────
        SELECT
            N'Documents',
            d.DocumentId,
            d.Title,
            ISNULL(d.DocumentType, N'Документ')
                + ISNULL(N' · ' + d.FileSize, N''),
            g.GroupName
        FROM dbo.Documents d
        JOIN dbo.Groups g ON d.GroupId = g.GroupId
        WHERE d.IsDeleted = 1
          AND (@TableFilter IS NULL OR @TableFilter = N'Documents')

        UNION ALL

        -- ── Родители / опекуны ────────────────────────────────────────────
        SELECT
            N'Parents',
            p.ParentId,
            p.LastName + N' ' + p.FirstName
                + ISNULL(N' ' + p.MiddleName, N''),
            p.Relation,
            us.LastName + N' ' + us.FirstName  -- студент, к которому привязан
        FROM dbo.Parents  p
        JOIN dbo.Students s  ON p.StudentId = s.StudentId
        JOIN dbo.Users    us ON s.UserId    = us.UserId
        WHERE p.IsDeleted = 1
          AND (@TableFilter IS NULL OR @TableFilter = N'Parents')
    ) AS src

    LEFT JOIN LastDelete ld
        ON  ld.TableName = src.TableName
        AND ld.RecordId  = src.RecordId
        AND ld.rn        = 1

    LEFT JOIN dbo.Users du
        ON du.UserId = ld.DeletedByUserId

    ORDER BY ISNULL(ld.DeletedAt, '2000-01-01') DESC;
END;
GO
