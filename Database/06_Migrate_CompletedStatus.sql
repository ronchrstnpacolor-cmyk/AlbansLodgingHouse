-- One-time migration to allow the 'Completed' status (guest checked out)
-- on Alban.tBookingForm. Safe to re-run.

USE AlbansLodgingHouse;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_tBookingForm_Status')
BEGIN
    ALTER TABLE Alban.tBookingForm DROP CONSTRAINT CK_tBookingForm_Status;
END
GO

ALTER TABLE Alban.tBookingForm ADD CONSTRAINT CK_tBookingForm_Status
    CHECK (Status IN ('New', 'Approved', 'Disapproved', 'Confirmed', 'Completed'));
GO
