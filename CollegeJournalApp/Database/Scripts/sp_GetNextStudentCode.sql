USE CollegeJournal;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetNextStudentCode
    @GroupId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Название группы (например "ЭБ-31")
    DECLARE @GroupName NVARCHAR(50);
    SELECT @GroupName = GroupName FROM dbo.Groups WHERE GroupId = @GroupId;

    IF @GroupName IS NULL
    BEGIN
        SELECT N'' AS NextCode;
        RETURN;
    END

    -- Префикс зачётки = название группы
    DECLARE @Prefix NVARCHAR(60) = @GroupName + N'-';

    -- Максимальный порядковый номер среди существующих кодов этой группы
    DECLARE @MaxNum INT = 0;

    SELECT @MaxNum = ISNULL(MAX(
        TRY_CAST(RIGHT(StudentCode, 3) AS INT)
    ), 0)
    FROM dbo.Students
    WHERE StudentCode LIKE @Prefix + '%'
      AND LEN(StudentCode) = LEN(@Prefix) + 3
      AND IsDeleted = 0;

    DECLARE @NextNum INT = @MaxNum + 1;

    -- Формат: "ЭБ-31-001"
    SELECT @GroupName + N'-' + RIGHT('000' + CAST(@NextNum AS NVARCHAR(3)), 3) AS NextCode;
END;
GO
