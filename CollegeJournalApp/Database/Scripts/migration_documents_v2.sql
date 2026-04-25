USE CollegeJournal;
GO

-- ============================================================
-- Личные документы студента (бинарные, с контролем доступа)
-- ============================================================
IF OBJECT_ID('dbo.StudentPersonalDocuments','U') IS NULL
CREATE TABLE dbo.StudentPersonalDocuments
(
    DocId        INT IDENTITY(1,1) PRIMARY KEY,
    StudentId    INT            NOT NULL REFERENCES dbo.Students(StudentId),
    Title        NVARCHAR(200)  NOT NULL,
    DocType      NVARCHAR(50)   NOT NULL DEFAULT N'Прочее',
    FileName     NVARCHAR(255)  NOT NULL,
    FileData     VARBINARY(MAX) NOT NULL,
    MimeType     NVARCHAR(100)  NULL,
    FileSizeKB   INT            NULL,
    Description  NVARCHAR(500)  NULL,
    UploadedById INT            NULL REFERENCES dbo.Users(UserId),
    UploadedAt   DATETIME       NOT NULL DEFAULT GETDATE(),
    IsDeleted    BIT            NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- Общие документы колледжа (загружает только Админ)
-- ============================================================
IF OBJECT_ID('dbo.GeneralDocuments','U') IS NULL
CREATE TABLE dbo.GeneralDocuments
(
    DocId        INT IDENTITY(1,1) PRIMARY KEY,
    Title        NVARCHAR(200)  NOT NULL,
    DocType      NVARCHAR(50)   NOT NULL DEFAULT N'Прочее',
    FileName     NVARCHAR(255)  NOT NULL,
    FileData     VARBINARY(MAX) NOT NULL,
    MimeType     NVARCHAR(100)  NULL,
    FileSizeKB   INT            NULL,
    Description  NVARCHAR(500)  NULL,
    UploadedById INT            NULL REFERENCES dbo.Users(UserId),
    UploadedAt   DATETIME       NOT NULL DEFAULT GETDATE(),
    IsDeleted    BIT            NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- Запросы куратора на доступ к личным документам студента
-- ============================================================
IF OBJECT_ID('dbo.DocumentAccessRequests','U') IS NULL
CREATE TABLE dbo.DocumentAccessRequests
(
    RequestId    INT IDENTITY(1,1) PRIMARY KEY,
    CuratorId    INT          NOT NULL REFERENCES dbo.Users(UserId),
    StudentId    INT          NOT NULL REFERENCES dbo.Students(StudentId),
    RequestedAt  DATETIME     NOT NULL DEFAULT GETDATE(),
    Status       NVARCHAR(20) NOT NULL DEFAULT N'Pending',  -- Pending | Approved | Denied
    ReviewedById INT          NULL REFERENCES dbo.Users(UserId),
    ReviewedAt   DATETIME     NULL,
    ExpiresAt    DATETIME     NULL   -- заполняется при одобрении
);
GO

-- ============================================================
-- Личные уведомления (запрос одобрен/отклонён, новый запрос)
-- ============================================================
IF OBJECT_ID('dbo.DocumentNotifications','U') IS NULL
CREATE TABLE dbo.DocumentNotifications
(
    NotifId   INT IDENTITY(1,1) PRIMARY KEY,
    ToUserId  INT           NOT NULL REFERENCES dbo.Users(UserId),
    Title     NVARCHAR(200) NOT NULL,
    Message   NVARCHAR(500) NULL,
    CreatedAt DATETIME      NOT NULL DEFAULT GETDATE(),
    IsRead    BIT           NOT NULL DEFAULT 0
);
GO
