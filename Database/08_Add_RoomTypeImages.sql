-- Adds Alban.tRoomTypeImage — up to 5 guest-facing photos per room type,
-- shown on the public site's Rooms & Dormitories cards. Safe to re-run.

USE AlbansLodgingHouse;
GO

IF OBJECT_ID('Alban.tRoomTypeImage', 'U') IS NULL
BEGIN
    CREATE TABLE Alban.tRoomTypeImage
    (
        RecordNo        INT IDENTITY(1,1)     NOT NULL,
        NewID           UNIQUEIDENTIFIER      NOT NULL CONSTRAINT DF_tRoomTypeImage_NewID DEFAULT NEWID(),
        RoomTypeNewID   UNIQUEIDENTIFIER      NOT NULL,
        ImagePath       NVARCHAR(300)         NOT NULL,
        SortOrder       INT                   NOT NULL CONSTRAINT DF_tRoomTypeImage_SortOrder DEFAULT 0,
        CreatedBy       NVARCHAR(120)         NULL,
        DateCreated     DATETIME2             NOT NULL CONSTRAINT DF_tRoomTypeImage_DateCreated DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_tRoomTypeImage PRIMARY KEY CLUSTERED (RecordNo),
        CONSTRAINT UQ_tRoomTypeImage_NewID UNIQUE (NewID),
        CONSTRAINT FK_tRoomTypeImage_RoomType FOREIGN KEY (RoomTypeNewID)
            REFERENCES Alban.tRoomType (NewID) ON DELETE CASCADE
    );

    CREATE INDEX IX_tRoomTypeImage_RoomTypeNewID ON Alban.tRoomTypeImage (RoomTypeNewID);
END
GO
