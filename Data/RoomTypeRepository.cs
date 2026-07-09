using AlbansLodgingHouse.Models;
using Microsoft.Data.SqlClient;

namespace AlbansLodgingHouse.Data;

public class RoomTypeRepository
{
    private readonly string _connectionString;

    public RoomTypeRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AlbansDb")
            ?? throw new InvalidOperationException("Connection string 'AlbansDb' is not configured.");
    }

    public async Task<IReadOnlyList<RoomType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await QueryAsync(
            """
            SELECT RecordNo, NewID, RoomType, Description, PriceRate, Term, Beds, Pax, RoomsAvailable, DateAvailable,
                   Discount, IsActive, CreatedBy, DateCreated, ModifiedBy, DateModified
            FROM Alban.tRoomType
            ORDER BY IsActive DESC, RoomType, Term
            """,
            cancellationToken);
    }

    public async Task<IReadOnlyList<RoomType>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await QueryAsync(
            """
            SELECT RecordNo, NewID, RoomType, Description, PriceRate, Term, Beds, Pax, RoomsAvailable, DateAvailable,
                   Discount, IsActive, CreatedBy, DateCreated, ModifiedBy, DateModified
            FROM Alban.tRoomType
            WHERE IsActive = 1
            ORDER BY RoomType, Term
            """,
            cancellationToken);
    }

    private async Task<IReadOnlyList<RoomType>> QueryAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);

        var results = new List<RoomType>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new RoomType
            {
                RecordNo = reader.GetInt32(reader.GetOrdinal("RecordNo")),
                NewID = reader.GetGuid(reader.GetOrdinal("NewID")),
                RoomTypeName = reader.GetString(reader.GetOrdinal("RoomType")),
                Description = GetNullableString(reader, "Description"),
                PriceRate = reader.GetDecimal(reader.GetOrdinal("PriceRate")),
                Term = reader.GetString(reader.GetOrdinal("Term")),
                Beds = reader.GetInt32(reader.GetOrdinal("Beds")),
                Pax = reader.GetInt32(reader.GetOrdinal("Pax")),
                RoomsAvailable = reader.GetInt32(reader.GetOrdinal("RoomsAvailable")),
                DateAvailable = GetNullableDateOnly(reader, "DateAvailable"),
                Discount = reader.GetDecimal(reader.GetOrdinal("Discount")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedBy = GetNullableString(reader, "CreatedBy"),
                DateCreated = reader.GetDateTime(reader.GetOrdinal("DateCreated")),
                ModifiedBy = GetNullableString(reader, "ModifiedBy"),
                DateModified = GetNullableDateTime(reader, "DateModified"),
            });
        }

        return results;
    }

    public async Task<Guid> InsertAsync(
        string roomType,
        string? description,
        decimal priceRate,
        string term,
        int beds,
        int pax,
        int roomsAvailable,
        DateOnly? dateAvailable,
        decimal discount,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            INSERT INTO Alban.tRoomType (RoomType, Description, PriceRate, Term, Beds, Pax, RoomsAvailable, DateAvailable, Discount, CreatedBy)
            OUTPUT INSERTED.NewID
            VALUES (@RoomType, @Description, @PriceRate, @Term, @Beds, @Pax, @RoomsAvailable, @DateAvailable, @Discount, @CreatedBy)
            """,
            connection);

        command.Parameters.AddWithValue("@RoomType", roomType);
        command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
        command.Parameters.AddWithValue("@PriceRate", priceRate);
        command.Parameters.AddWithValue("@Term", term);
        command.Parameters.AddWithValue("@Beds", beds);
        command.Parameters.AddWithValue("@Pax", pax);
        command.Parameters.AddWithValue("@RoomsAvailable", roomsAvailable);
        command.Parameters.AddWithValue("@DateAvailable", (object?)dateAvailable?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@Discount", discount);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);

        return (Guid)await command.ExecuteScalarAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Guid newId,
        string roomType,
        string? description,
        decimal priceRate,
        string term,
        int beds,
        int pax,
        int roomsAvailable,
        DateOnly? dateAvailable,
        decimal discount,
        string modifiedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            UPDATE Alban.tRoomType
            SET RoomType = @RoomType,
                Description = @Description,
                PriceRate = @PriceRate,
                Term = @Term,
                Beds = @Beds,
                Pax = @Pax,
                RoomsAvailable = @RoomsAvailable,
                DateAvailable = @DateAvailable,
                Discount = @Discount,
                ModifiedBy = @ModifiedBy,
                DateModified = SYSUTCDATETIME()
            WHERE NewID = @NewID
            """,
            connection);

        command.Parameters.AddWithValue("@RoomType", roomType);
        command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
        command.Parameters.AddWithValue("@PriceRate", priceRate);
        command.Parameters.AddWithValue("@Term", term);
        command.Parameters.AddWithValue("@Beds", beds);
        command.Parameters.AddWithValue("@Pax", pax);
        command.Parameters.AddWithValue("@RoomsAvailable", roomsAvailable);
        command.Parameters.AddWithValue("@DateAvailable", (object?)dateAvailable?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@Discount", discount);
        command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
        command.Parameters.AddWithValue("@NewID", newId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid newId, bool isActive, string modifiedBy, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            UPDATE Alban.tRoomType
            SET IsActive = @IsActive, ModifiedBy = @ModifiedBy, DateModified = SYSUTCDATETIME()
            WHERE NewID = @NewID
            """,
            connection);

        command.Parameters.AddWithValue("@IsActive", isActive);
        command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
        command.Parameters.AddWithValue("@NewID", newId);

        await command.ExecuteNonQueryAsync(cancellationToken);
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
