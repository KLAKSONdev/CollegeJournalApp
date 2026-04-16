USE CollegeJournal;
GO
CREATE OR ALTER PROCEDURE dbo.sp_GetDormitories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        d.DormitoryId,
        d.Name,
        ISNULL(d.Address,        N'—') AS Address,
        ISNULL(d.CommandantName, N'—') AS CommandantName,
        ISNULL(d.Phone,          N'—') AS Phone,
        d.TotalRooms,
        (SELECT COUNT(*) FROM dbo.Students WHERE DormitoryId = d.DormitoryId AND IsDeleted = 0) AS ResidentsCount
    FROM dbo.Dormitories d
    WHERE d.IsDeleted = 0
    ORDER BY d.Name;
END;
GO
