USE CollegeJournal;
GO

-- Все ожидающие запросы на доступ — для администратора.
CREATE OR ALTER PROCEDURE dbo.sp_GetPendingAccessRequests
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.RequestId,
        r.CuratorId,
        cu.LastName + N' ' + cu.FirstName + ISNULL(N' ' + cu.MiddleName, N'') AS CuratorName,
        r.StudentId,
        su.LastName + N' ' + su.FirstName + ISNULL(N' ' + su.MiddleName, N'') AS StudentName,
        g.GroupName,
        r.RequestedAt
    FROM dbo.DocumentAccessRequests r
    JOIN dbo.Users    cu ON cu.UserId    = r.CuratorId
    JOIN dbo.Students s  ON s.StudentId  = r.StudentId
    JOIN dbo.Users    su ON su.UserId    = s.UserId
    JOIN dbo.Groups   g  ON g.GroupId    = s.GroupId
    WHERE r.Status = N'Pending'
    ORDER BY r.RequestedAt ASC;
END;
GO
