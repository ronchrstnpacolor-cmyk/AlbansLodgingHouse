-- One-time migration to add a guest-facing Description column to
-- Alban.tRoomType, shown on the public site's room cards. Safe to re-run.

USE AlbansLodgingHouse;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Alban.tRoomType') AND name = 'Description'
)
BEGIN
    ALTER TABLE Alban.tRoomType
        ADD Description NVARCHAR(500) NULL;
END
GO
