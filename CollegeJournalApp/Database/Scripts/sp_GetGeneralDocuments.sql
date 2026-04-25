USE CollegeJournal;
GO

-- Список общих документов колледжа (метаданные, без FileData).
CREATE OR ALTER PROCEDURE dbo.sp_GetGeneralDocuments
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DocId,
        d.Title,
        d.DocType,
        d.FileName,
        d.MimeType,
        ISNULL(d.FileSizeKB, 0)                        AS FileSizeKB,
        ISNULL(d.Description, N'')                     AS Description,
        d.UploadedAt,
        ISNULL(u.LastName + N' ' + u.FirstName, N'—')  AS UploadedByName
    FROM dbo.GeneralDocuments d
    LEFT JOIN dbo.Users u ON u.UserId = d.UploadedById
    WHERE d.IsDeleted = 0
    ORDER BY d.UploadedAt DESC;
END;
GO
