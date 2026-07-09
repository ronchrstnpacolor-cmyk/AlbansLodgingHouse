-- Alban's Lodging House - booking database schema
-- Run against a SQL Server instance. Safe to re-run (idempotent).

IF DB_ID('AlbansLodgingHouse') IS NULL
BEGIN
    CREATE DATABASE AlbansLodgingHouse;
END
GO

USE AlbansLodgingHouse;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Alban')
BEGIN
    EXEC('CREATE SCHEMA Alban');
END
GO

-- ---------------------------------------------------------------------
-- Alban.tBookingForm — client-submitted reservation requests
-- ---------------------------------------------------------------------
IF OBJECT_ID('Alban.tBookingForm', 'U') IS NULL
BEGIN
    CREATE TABLE Alban.tBookingForm
    (
        RecordNo            INT IDENTITY(1,1)     NOT NULL,
        NewID                UNIQUEIDENTIFIER      NOT NULL CONSTRAINT DF_tBookingForm_NewID DEFAULT NEWID(),
        BookingReferenceNo   VARCHAR(20)           NOT NULL,
        FullName             NVARCHAR(120)         NOT NULL,
        PhoneNo              VARCHAR(40)           NULL,
        Email                NVARCHAR(160)         NULL,
        CheckIn              DATE                  NULL,
        CheckOut             DATE                  NULL,
        RoomType             NVARCHAR(80)          NULL,
        Pax                  INT                   NOT NULL CONSTRAINT DF_tBookingForm_Pax DEFAULT 1,
        Message              NVARCHAR(1000)        NULL,
        Status               VARCHAR(20)           NOT NULL CONSTRAINT DF_tBookingForm_Status DEFAULT 'New',
        DateCreated          DATETIME2             NOT NULL CONSTRAINT DF_tBookingForm_DateCreated DEFAULT SYSUTCDATETIME(),
        ModifiedBy           NVARCHAR(120)         NULL,
        DateModified         DATETIME2             NULL,

        CONSTRAINT PK_tBookingForm PRIMARY KEY CLUSTERED (RecordNo),
        CONSTRAINT UQ_tBookingForm_NewID UNIQUE (NewID),
        CONSTRAINT UQ_tBookingForm_BookingReferenceNo UNIQUE (BookingReferenceNo),
        -- New: submitted by client, awaiting management review.
        -- Approved / Disapproved: management decision recorded in tBookingApproval.
        -- Confirmed: client clicked the confirmation link in the approval email.
        -- Completed: guest has checked out (management marks this manually).
        CONSTRAINT CK_tBookingForm_Status CHECK (Status IN ('New', 'Approved', 'Disapproved', 'Confirmed', 'Completed'))
    );
END
GO

-- ---------------------------------------------------------------------
-- Alban.tBookingApproval — management decision log for a booking
-- ---------------------------------------------------------------------
IF OBJECT_ID('Alban.tBookingApproval', 'U') IS NULL
BEGIN
    CREATE TABLE Alban.tBookingApproval
    (
        RecordNo            INT IDENTITY(1,1)     NOT NULL,
        NewID                UNIQUEIDENTIFIER      NOT NULL CONSTRAINT DF_tBookingApproval_NewID DEFAULT NEWID(),
        BookingReferenceNo   VARCHAR(20)           NOT NULL,
        Status               VARCHAR(20)           NOT NULL,
        Remarks              NVARCHAR(500)         NULL,
        CreatedBy            NVARCHAR(120)         NOT NULL,
        DateCreated          DATETIME2             NOT NULL CONSTRAINT DF_tBookingApproval_DateCreated DEFAULT SYSUTCDATETIME(),
        ModifiedBy           NVARCHAR(120)         NULL,
        DateModified         DATETIME2             NULL,

        CONSTRAINT PK_tBookingApproval PRIMARY KEY CLUSTERED (RecordNo),
        CONSTRAINT UQ_tBookingApproval_NewID UNIQUE (NewID),
        CONSTRAINT CK_tBookingApproval_Status CHECK (Status IN ('Approved', 'Disapproved')),
        CONSTRAINT FK_tBookingApproval_tBookingForm FOREIGN KEY (BookingReferenceNo)
            REFERENCES Alban.tBookingForm (BookingReferenceNo)
    );

    CREATE INDEX IX_tBookingApproval_BookingReferenceNo ON Alban.tBookingApproval (BookingReferenceNo);
END
GO

-- ---------------------------------------------------------------------
-- Alban.tManagementAccess — hostnames allowed to see the Management form
-- ---------------------------------------------------------------------
IF OBJECT_ID('Alban.tManagementAccess', 'U') IS NULL
BEGIN
    CREATE TABLE Alban.tManagementAccess
    (
        RecordNo      INT IDENTITY(1,1)   NOT NULL,
        HostName      NVARCHAR(255)       NOT NULL,
        IsActive      BIT                 NOT NULL CONSTRAINT DF_tManagementAccess_IsActive DEFAULT 1,
        Description   NVARCHAR(200)       NULL,
        CreatedBy     NVARCHAR(120)       NULL,
        DateCreated   DATETIME2           NOT NULL CONSTRAINT DF_tManagementAccess_DateCreated DEFAULT SYSUTCDATETIME(),
        ModifiedBy    NVARCHAR(120)       NULL,
        DateModified  DATETIME2           NULL,

        CONSTRAINT PK_tManagementAccess PRIMARY KEY CLUSTERED (RecordNo),
        CONSTRAINT UQ_tManagementAccess_HostName UNIQUE (HostName)
    );

    -- Seed with local dev hostnames so the Management page is reachable out of the box.
    INSERT INTO Alban.tManagementAccess (HostName, IsActive, Description, CreatedBy)
    VALUES
        ('localhost', 1, 'Local development machine', 'system'),
        ('127.0.0.1', 1, 'Local development machine (loopback IP)', 'system'),
        ('ronchrstn', 1, 'User machine hostname', 'system');
END
GO

-- ---------------------------------------------------------------------
-- Alban.tManagementEmail — addresses notified about booking activity
-- (new submissions, and clients confirming an approved booking)
-- ---------------------------------------------------------------------
IF OBJECT_ID('Alban.tManagementEmail', 'U') IS NULL
BEGIN
    CREATE TABLE Alban.tManagementEmail
    (
        RecordNo      INT IDENTITY(1,1)   NOT NULL,
        Email         NVARCHAR(160)       NOT NULL,
        FullName      NVARCHAR(120)       NULL,
        IsActive      BIT                 NOT NULL CONSTRAINT DF_tManagementEmail_IsActive DEFAULT 1,
        CreatedBy     NVARCHAR(120)       NULL,
        DateCreated   DATETIME2           NOT NULL CONSTRAINT DF_tManagementEmail_DateCreated DEFAULT SYSUTCDATETIME(),
        ModifiedBy    NVARCHAR(120)       NULL,
        DateModified  DATETIME2           NULL,

        CONSTRAINT PK_tManagementEmail PRIMARY KEY CLUSTERED (RecordNo),
        CONSTRAINT UQ_tManagementEmail_Email UNIQUE (Email)
    );

    INSERT INTO Alban.tManagementEmail (Email, FullName, IsActive, CreatedBy)
    VALUES ('albans.booking@gmail.com', 'Alban''s Lodging House Management', 1, 'system');
END
GO
