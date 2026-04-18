USE CollegeJournal;
GO

-- Мягкое удаление записи посещаемости. Разрешено только Admin.
CREATE OR ALTER PROCEDURE dbo.sp_DeleteAttendanceMark
    @AttendanceId INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IsAdmin BIT = 0;
    SELECT @IsAdmin = CASE WHEN r.RoleName = 'Admin' THEN 1 ELSE 0 END
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE u.UserId = @UserId AND u.IsDeleted = 0;

    IF @IsAdmin = 1
        UPDATE dbo.Attendance SET IsDeleted = 1 WHERE AttendanceId = @AttendanceId;
    ELSE
        RAISERROR(N'Недостаточно прав для удаления записи.', 16, 1);
END;
GO
