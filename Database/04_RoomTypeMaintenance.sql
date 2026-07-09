-- Alban's Lodging House - room type maintenance / inventory table
-- Run after 01_Schema.sql. Safe to re-run (idempotent).

USE AlbansLodgingHouse;
GO

-- ---------------------------------------------------------------------
-- Alban.tRoomType — room inventory maintained by management: room
-- type, rate, term, capacity, availability and discount.
-- ---------------------------------------------------------------------
IF OBJECT_ID('Alban.tRoomType', 'U') IS NULL
BEGIN
    CREATE TABLE Alban.tRoomType
    (
        RecordNo        INT IDENTITY(1,1)     NOT NULL,
        NewID           UNIQUEIDENTIFIER      NOT NULL CONSTRAINT DF_tRoomType_NewID DEFAULT NEWID(),
        RoomType        NVARCHAR(80)          NOT NULL,
        PriceRate       DECIMAL(10,2)         NOT NULL,
        Term            VARCHAR(20)           NOT NULL,
        Beds            INT                   NOT NULL,
        Pax             INT                   NOT NULL,
        RoomsAvailable  INT                   NOT NULL CONSTRAINT DF_tRoomType_RoomsAvailable DEFAULT 1,
        DateAvailable   DATE                  NULL,
        Discount        DECIMAL(5,2)          NOT NULL CONSTRAINT DF_tRoomType_Discount DEFAULT 0,
        IsActive        BIT                   NOT NULL CONSTRAINT DF_tRoomType_IsActive DEFAULT 1,
        CreatedBy       NVARCHAR(120)         NULL,
        DateCreated     DATETIME2             NOT NULL CONSTRAINT DF_tRoomType_DateCreated DEFAULT SYSUTCDATETIME(),
        ModifiedBy      NVARCHAR(120)         NULL,
        DateModified    DATETIME2             NULL,

        CONSTRAINT PK_tRoomType PRIMARY KEY CLUSTERED (RecordNo),
        CONSTRAINT UQ_tRoomType_NewID UNIQUE (NewID),
        CONSTRAINT CK_tRoomType_Term CHECK (Term IN ('Short Term', 'Long Term')),
        CONSTRAINT CK_tRoomType_Beds CHECK (Beds > 0),
        CONSTRAINT CK_tRoomType_Pax CHECK (Pax > 0),
        CONSTRAINT CK_tRoomType_RoomsAvailable CHECK (RoomsAvailable >= 0),
        CONSTRAINT CK_tRoomType_Discount CHECK (Discount >= 0)
    );
END
GO
