using AlbansLodgingHouse.Models;
using Microsoft.Data.SqlClient;

namespace AlbansLodgingHouse.Data;

public class BookingRepository
{
    private readonly string _connectionString;

    public BookingRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AlbansDb")
            ?? throw new InvalidOperationException("Connection string 'AlbansDb' is not configured.");
    }

    public async Task<BookingFormRecord> InsertBookingAsync(
        string fullName,
        string? phoneNo,
        string? email,
        DateOnly? checkIn,
        DateOnly? checkOut,
        string? roomType,
        int pax,
        string? message,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("Alban.spBookingForm_Insert", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
        };

        command.Parameters.AddWithValue("@FullName", fullName);
        command.Parameters.AddWithValue("@PhoneNo", (object?)phoneNo ?? DBNull.Value);
        command.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
        command.Parameters.AddWithValue("@CheckIn", (object?)checkIn?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@CheckOut", (object?)checkOut?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@RoomType", (object?)roomType ?? DBNull.Value);
        command.Parameters.AddWithValue("@Pax", pax);
        command.Parameters.AddWithValue("@Message", (object?)message ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return MapBookingFormRecord(reader);
    }

    public async Task<IReadOnlyList<BookingFormRecord>> GetNewBookingsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("Alban.spBookingForm_GetNew", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
        };

        var results = new List<BookingFormRecord>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapBookingFormRecord(reader));
        }

        return results;
    }

    public async Task<IReadOnlyList<BookingFormRecord>> GetAllBookingsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT RecordNo, NewID, BookingReferenceNo, FullName, PhoneNo, Email,
                   CheckIn, CheckOut, RoomType, Pax, Message, Status,
                   DateCreated, ModifiedBy, DateModified
            FROM Alban.tBookingForm
            ORDER BY DateCreated DESC
            """,
            connection);

        var results = new List<BookingFormRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapBookingFormRecord(reader));
        }

        return results;
    }

    public async Task<BookingFormRecord?> GetByNewIdAsync(Guid newId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT RecordNo, NewID, BookingReferenceNo, FullName, PhoneNo, Email,
                   CheckIn, CheckOut, RoomType, Pax, Message, Status,
                   DateCreated, ModifiedBy, DateModified
            FROM Alban.tBookingForm
            WHERE NewID = @NewID
            """,
            connection);
        command.Parameters.AddWithValue("@NewID", newId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return MapBookingFormRecord(reader);
    }

    public async Task<BookingFormRecord?> GetByReferenceNoAsync(string bookingReferenceNo, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT RecordNo, NewID, BookingReferenceNo, FullName, PhoneNo, Email,
                   CheckIn, CheckOut, RoomType, Pax, Message, Status,
                   DateCreated, ModifiedBy, DateModified
            FROM Alban.tBookingForm
            WHERE BookingReferenceNo = @BookingReferenceNo
            """,
            connection);
        command.Parameters.AddWithValue("@BookingReferenceNo", bookingReferenceNo);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return MapBookingFormRecord(reader);
    }

    public async Task<BookingFormRecord> ConfirmAsync(Guid newId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("Alban.spBookingForm_Confirm", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("@NewID", newId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return MapBookingFormRecord(reader);
    }

    public async Task<BookingFormRecord> CheckoutAsync(Guid newId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("Alban.spBookingForm_Checkout", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("@NewID", newId);
        command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return MapBookingFormRecord(reader);
    }

    public async Task<BookingApprovalResult> InsertApprovalAsync(
        string bookingReferenceNo,
        string status,
        string? remarks,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("Alban.spBookingApproval_Insert", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
        };

        command.Parameters.AddWithValue("@BookingReferenceNo", bookingReferenceNo);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return new BookingApprovalResult
        {
            RecordNo = reader.GetInt32(reader.GetOrdinal("RecordNo")),
            NewID = reader.GetGuid(reader.GetOrdinal("NewID")),
            BookingReferenceNo = reader.GetString(reader.GetOrdinal("BookingReferenceNo")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? null : reader.GetString(reader.GetOrdinal("Remarks")),
            CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
        };
    }

    private static BookingFormRecord MapBookingFormRecord(SqlDataReader reader)
    {
        return new BookingFormRecord
        {
            RecordNo = reader.GetInt32(reader.GetOrdinal("RecordNo")),
            NewID = reader.GetGuid(reader.GetOrdinal("NewID")),
            BookingReferenceNo = reader.GetString(reader.GetOrdinal("BookingReferenceNo")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            PhoneNo = GetNullableString(reader, "PhoneNo"),
            Email = GetNullableString(reader, "Email"),
            CheckIn = GetNullableDateOnly(reader, "CheckIn"),
            CheckOut = GetNullableDateOnly(reader, "CheckOut"),
            RoomType = GetNullableString(reader, "RoomType"),
            Pax = reader.GetInt32(reader.GetOrdinal("Pax")),
            Message = GetNullableString(reader, "Message"),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            DateCreated = HasColumn(reader, "DateCreated") ? reader.GetDateTime(reader.GetOrdinal("DateCreated")) : default,
            ModifiedBy = HasColumn(reader, "ModifiedBy") ? GetNullableString(reader, "ModifiedBy") : null,
            DateModified = HasColumn(reader, "DateModified") ? GetNullableDateTime(reader, "DateModified") : null,
        };
    }

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateOnly? GetNullableDateOnly(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : DateOnly.FromDateTime(reader.GetDateTime(ordinal));
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
