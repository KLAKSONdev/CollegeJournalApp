USE CollegeJournal;
GO

IF OBJECT_ID('dbo.sp_DeleteScheduleItem', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteScheduleItem;
GO

CREATE PROCEDURE [dbo].[sp_DeleteScheduleItem]
    @ScheduleId  INT,
    @DeletedById INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Schedule
    SET IsDeleted   = 1,
        DeletedAt   = GETDATE(),
        DeletedById = @DeletedById
    WHERE ScheduleId = @ScheduleId;
END
GO
