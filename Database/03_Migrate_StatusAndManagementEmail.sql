-- One-time migration for a database created before the New/Confirmed status
-- values and the tManagementEmail table existed. Safe to re-run.

USE AlbansLodgingHouse;
GO

-- ---------------------------------------------------------------------
-- Rename 'Pending' -> 'New' and allow 'Confirmed' on Alban.tBookingForm
-- ---------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_tBookingForm_Status')
BEGIN
    ALTER TABLE Alban.tBookingForm DROP CONSTRAINT CK_tBookingForm_Status;
END
GO

IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_tBookingForm_Status')
BEGIN
    ALTER TABLE Alban.tBookingForm DROP CONSTRAINT DF_tBookingForm_Status;
END
GO

UPDATE Alban.tBookingForm SET Status = 'New' WHERE Status = 'Pending';
GO

ALTER TABLE Alban.tBookingForm ADD CONSTRAINT DF_tBookingForm_Status DEFAULT 'New' FOR Status;
GO

ALTER TABLE Alban.tBookingForm ADD CONSTRAINT CK_tBookingForm_Status
    CHECK (Status IN ('New', 'Approved', 'Disapproved', 'Confirmed'));
GO

-- ---------------------------------------------------------------------
-- Alban.tManagementEmail — addresses notified about booking activity
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
