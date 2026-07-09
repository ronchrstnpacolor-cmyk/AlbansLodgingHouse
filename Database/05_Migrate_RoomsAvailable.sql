-- One-time migration for a tRoomType table created before RoomsAvailable
-- existed. Safe to re-run.

USE AlbansLodgingHouse;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Alban.tRoomType') AND name = 'RoomsAvailable'
)
BEGIN
    ALTER TABLE Alban.tRoomType
        ADD RoomsAvailable INT NOT NULL CONSTRAINT DF_tRoomType_RoomsAvailable DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_tRoomType_RoomsAvailable')
BEGIN
    ALTER TABLE Alban.tRoomType
        ADD CONSTRAINT CK_tRoomType_RoomsAvailable CHECK (RoomsAvailable >= 0);
END
GO
