USE CollegeJournal;
GO

-- Возвращает статус доступа куратора к документам студента.
-- HasAccess = 1 только если Status = 'Approved' AND ExpiresAt > GETDATE()
CREATE OR ALTER PROCEDURE dbo.sp_GetDocumentAccess
    @CuratorId INT,
    @StudentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        r.RequestId,
        r.Status,
        r.ExpiresAt,
        CASE
            WHEN r.Status = N'Approved' AND r.ExpiresAt > GETDATE() THEN 1
            ELSE 0
        END AS HasAccess
    FROM dbo.DocumentAccessRequests r
    WHERE r.CuratorId = @CuratorId
      AND r.StudentId = @StudentId
    ORDER BY r.RequestedAt DESC;
END;
GO
