USE CollegeJournal;
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveParent
    @ParentId         INT           = NULL,
    @StudentId        INT,
    @Relation         NVARCHAR(30),
    @LastName         NVARCHAR(50),
    @FirstName        NVARCHAR(50),
    @MiddleName       NVARCHAR(50)  = NULL,
    @BirthDate        DATE          = NULL,
    @Phone            NVARCHAR(20)  = NULL,
    @WorkPhone        NVARCHAR(20)  = NULL,
    @Email            NVARCHAR(100) = NULL,
    @Address          NVARCHAR(300) = NULL,
    @Workplace        NVARCHAR(200) = NULL,
    @Position         NVARCHAR(100) = NULL,
    @Department       NVARCHAR(100) = NULL,
    @Education        NVARCHAR(50)  = NULL,
    @IsMainContact    BIT           = 0,
    @IsDeceased       BIT           = 0,
    @HasParentalRights BIT          = 1,
    @UpdatedById      INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @ParentId IS NULL OR @ParentId = 0
    BEGIN
        -- Добавление нового родителя
        INSERT INTO dbo.Parents
            (StudentId, Relation, LastName, FirstName, MiddleName, BirthDate,
             Phone, WorkPhone, Email, Address, Workplace, Position, Department,
             Education, IsMainContact, IsDeceased, HasParentalRights, IsDeleted)
        VALUES
            (@StudentId, @Relation, @LastName, @FirstName, @MiddleName, @BirthDate,
             @Phone, @WorkPhone, @Email, @Address, @Workplace, @Position, @Department,
             @Education, @IsMainContact, @IsDeceased, @HasParentalRights, 0);

        DECLARE @NewId INT = SCOPE_IDENTITY();

        IF @UpdatedById IS NOT NULL
            INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
            VALUES (@UpdatedById, N'CREATE', N'Parents', @NewId);
    END
    ELSE
    BEGIN
        -- Обновление существующего родителя
        UPDATE dbo.Parents
        SET Relation          = @Relation,
            LastName          = @LastName,
            FirstName         = @FirstName,
            MiddleName        = @MiddleName,
            BirthDate         = @BirthDate,
            Phone             = @Phone,
            WorkPhone         = @WorkPhone,
            Email             = @Email,
            Address           = @Address,
            Workplace         = @Workplace,
            Position          = @Position,
            Department        = @Department,
            Education         = @Education,
            IsMainContact     = @IsMainContact,
            IsDeceased        = @IsDeceased,
            HasParentalRights = @HasParentalRights
        WHERE ParentId = @ParentId
          AND IsDeleted = 0;

        IF @UpdatedById IS NOT NULL
            INSERT INTO dbo.AuditLog (UserId, Action, TableName, RecordId)
            VALUES (@UpdatedById, N'UPDATE', N'Parents', @ParentId);
    END
END;
GO
