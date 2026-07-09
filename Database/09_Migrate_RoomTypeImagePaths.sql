-- Normalizes Alban.tRoomTypeImage.ImagePath to always be ready-to-embed:
-- root-relative ("/uploads/...") for local disk storage, or an absolute
-- URL (http/https) for cloud (R2) storage. Earlier rows were stored
-- without the leading slash. Safe to re-run.

USE AlbansLodgingHouse;
GO

UPDATE Alban.tRoomTypeImage
SET ImagePath = '/' + ImagePath
WHERE ImagePath NOT LIKE '/%'
  AND ImagePath NOT LIKE 'http://%'
  AND ImagePath NOT LIKE 'https://%';
GO
