-- Alban's Lodging House - booking stored procedures
-- Run after 01_Schema.sql. Safe to re-run (CREATE OR ALTER).

USE AlbansLodgingHouse;
GO

-- ---------------------------------------------------------------------
-- Alban.spBookingForm_Insert
-- Inserts a new reservation request and returns the generated
-- BookingReferenceNo/NewID along with the submitted details, so the
-- caller can build the QR code / confirmation screen.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE Alban.spBookingForm_Insert
    @FullName    NVARCHAR(120),
    @PhoneNo     VARCHAR(40)     = NULL,
    @Email       NVARCHAR(160)   = NULL,
    @CheckIn     DATE            = NULL,
    @CheckOut    DATE            = NULL,
    @RoomType    NVARCHAR(80)    = NULL,
    @Pax         INT             = 1,
    @Message     NVARCHAR(1000)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Today CHAR(8) = FORMAT(SYSUTCDATETIME(), 'yyyyMMdd');
    DECLARE @NextSeq INT;
    DECLARE @BookingReferenceNo VARCHAR(20);
    DECLARE @RecordNo INT;
    DECLARE @NewID UNIQUEIDENTIFIER;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Serialize reference-number generation for the day so concurrent
        -- submissions never collide on the same BookingReferenceNo.
        SELECT @NextSeq = COUNT(*) + 1
        FROM Alban.tBookingForm WITH (TABLOCKX, HOLDLOCK)
        WHERE BookingReferenceNo LIKE 'ALH-' + @Today + '-%';

        SET @BookingReferenceNo = 'ALH-' + @Today + '-' + RIGHT('0000' + CAST(@NextSeq AS VARCHAR(10)), 4);

        INSERT INTO Alban.tBookingForm
            (BookingReferenceNo, FullName, PhoneNo, Email, CheckIn, CheckOut, RoomType, Pax, Message, Status)
        VALUES
            (@BookingReferenceNo, @FullName, @PhoneNo, @Email, @CheckIn, @CheckOut, @RoomType, @Pax, @Message, 'New');

        SET @RecordNo = SCOPE_IDENTITY();

        SELECT @NewID = NewID
        FROM Alban.tBookingForm
        WHERE RecordNo = @RecordNo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT
        @RecordNo            AS RecordNo,
        @NewID                AS NewID,
        @BookingReferenceNo   AS BookingReferenceNo,
        @FullName             AS FullName,
        @PhoneNo              AS PhoneNo,
        @Email                AS Email,
        @CheckIn              AS CheckIn,
        @CheckOut             AS CheckOut,
        @RoomType             AS RoomType,
        @Pax                  AS Pax,
        @Message              AS Message,
        'New'                 AS Status;
END
GO

IF OBJECT_ID('Alban.spBookingForm_GetPending', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE Alban.spBookingForm_GetPending;
END
GO

-- ---------------------------------------------------------------------
-- Alban.spBookingForm_GetNew
-- Lists bookings awaiting a management decision, oldest first.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE Alban.spBookingForm_GetNew
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        RecordNo, NewID, BookingReferenceNo, FullName, PhoneNo, Email,
        CheckIn, CheckOut, RoomType, Pax, Message, Status,
        DateCreated, ModifiedBy, DateModified
    FROM Alban.tBookingForm
    WHERE Status = 'New'
    ORDER BY DateCreated ASC;
END
GO

-- ---------------------------------------------------------------------
-- Alban.spBookingForm_Confirm
-- Called when a client clicks the confirmation link in their approval
-- email. Only an Approved booking can move to Confirmed. Returns the
-- booking so the caller can notify management.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE Alban.spBookingForm_Confirm
    @NewID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus VARCHAR(20);
    SELECT @CurrentStatus = Status FROM Alban.tBookingForm WHERE NewID = @NewID;

    IF @CurrentStatus IS NULL
    BEGIN
        RAISERROR('Booking was not found.', 16, 1);
        RETURN;
    END

    IF @CurrentStatus <> 'Approved'
    BEGIN
        RAISERROR('Booking cannot be confirmed from its current status (%s).', 16, 1, @CurrentStatus);
        RETURN;
    END

    UPDATE Alban.tBookingForm
    SET Status = 'Confirmed',
        ModifiedBy = 'Client Confirmation',
        DateModified = SYSUTCDATETIME()
    WHERE NewID = @NewID;

    SELECT
        RecordNo, NewID, BookingReferenceNo, FullName, PhoneNo, Email,
        CheckIn, CheckOut, RoomType, Pax, Message, Status,
        DateCreated, ModifiedBy, DateModified
    FROM Alban.tBookingForm
    WHERE NewID = @NewID;
END
GO

-- ---------------------------------------------------------------------
-- Alban.spBookingForm_Checkout
-- Marks a booking Completed once the guest has checked out. Allowed
-- from Approved (guest showed up without clicking the confirm link)
-- or Confirmed. Returns the updated booking.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE Alban.spBookingForm_Checkout
    @NewID      UNIQUEIDENTIFIER,
    @ModifiedBy NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus VARCHAR(20);
    SELECT @CurrentStatus = Status FROM Alban.tBookingForm WHERE NewID = @NewID;

    IF @CurrentStatus IS NULL
    BEGIN
        RAISERROR('Booking was not found.', 16, 1);
        RETURN;
    END

    IF @CurrentStatus NOT IN ('Approved', 'Confirmed')
    BEGIN
        RAISERROR('Booking cannot be checked out from its current status (%s).', 16, 1, @CurrentStatus);
        RETURN;
    END

    UPDATE Alban.tBookingForm
    SET Status = 'Completed',
        ModifiedBy = @ModifiedBy,
        DateModified = SYSUTCDATETIME()
    WHERE NewID = @NewID;

    SELECT
        RecordNo, NewID, BookingReferenceNo, FullName, PhoneNo, Email,
        CheckIn, CheckOut, RoomType, Pax, Message, Status,
        DateCreated, ModifiedBy, DateModified
    FROM Alban.tBookingForm
    WHERE NewID = @NewID;
END
GO

-- ---------------------------------------------------------------------
-- Alban.spBookingApproval_Insert
-- Records a management decision (Approved/Disapproved) for a booking
-- and updates the booking's Status/ModifiedBy/DateModified to match.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE Alban.spBookingApproval_Insert
    @BookingReferenceNo VARCHAR(20),
    @Status             VARCHAR(20),
    @Remarks            NVARCHAR(500) = NULL,
    @CreatedBy          NVARCHAR(120)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Status NOT IN ('Approved', 'Disapproved')
    BEGIN
        RAISERROR('Status must be ''Approved'' or ''Disapproved''.', 16, 1);
        RETURN;
    END

    IF @Status = 'Disapproved' AND LTRIM(RTRIM(ISNULL(@Remarks, ''))) = ''
    BEGIN
        RAISERROR('Remarks are required when disapproving a booking.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Alban.tBookingForm WHERE BookingReferenceNo = @BookingReferenceNo)
    BEGIN
        RAISERROR('Booking reference %s was not found.', 16, 1, @BookingReferenceNo);
        RETURN;
    END

    DECLARE @RecordNo INT;
    DECLARE @NewID UNIQUEIDENTIFIER;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Alban.tBookingApproval (BookingReferenceNo, Status, Remarks, CreatedBy)
        VALUES (@BookingReferenceNo, @Status, @Remarks, @CreatedBy);

        SET @RecordNo = SCOPE_IDENTITY();

        SELECT @NewID = NewID
        FROM Alban.tBookingApproval
        WHERE RecordNo = @RecordNo;

        UPDATE Alban.tBookingForm
        SET Status = @Status,
            ModifiedBy = @CreatedBy,
            DateModified = SYSUTCDATETIME()
        WHERE BookingReferenceNo = @BookingReferenceNo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT
        @RecordNo            AS RecordNo,
        @NewID                AS NewID,
        @BookingReferenceNo   AS BookingReferenceNo,
        @Status               AS Status,
        @Remarks              AS Remarks,
        @CreatedBy            AS CreatedBy;
END
GO
