using AlbansLodgingHouse.Models;
using Microsoft.Data.SqlClient;

namespace AlbansLodgingHouse.Data;

public class RoomTypeImageRepository
{
    private readonly string _connectionString;

    public RoomTypeImageRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AlbansDb")
            ?? throw new InvalidOperationException("Connection string 'AlbansDb' is not configured.");
    }

    public async Task<IReadOnlyList<RoomTypeImage>> GetForRoomTypeAsync(Guid roomTypeNewId, CancellationToken cancellationToken = default)
    {
        var all = await GetForRoomTypesAsync(new[] { roomTypeNewId }, cancellationToken);
        return all.TryGetValue(roomTypeNewId, out var images) ? images : Array.Empty<RoomTypeImage>();
    }

    public async Task<IReadOnlyDictionary<Guid, List<RoomTypeImage>>> GetForRoomTypesAsync(
        IEnumerable<Guid> roomTypeNewIds,
        CancellationToken cancellationToken = default)
    {
        var ids = roomTypeNewIds.Distinct().ToList();
        var result = new Dictionary<Guid, List<RoomTypeImage>>();
        if (ids.Count == 0) return result;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT RecordNo, NewID, RoomTypeNewID, ImagePath, SortOrder, CreatedBy, DateCreated
            FROM Alban.tRoomTypeImage
            WHERE RoomTypeNewID IN (SELECT value FROM STRING_SPLIT(@RoomTypeNewIds, ','))
            ORDER BY RoomTypeNewID, SortOrder, RecordNo
            """,
            connection);
        command.Parameters.AddWithValue("@RoomTypeNewIds", string.Join(',', ids));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var image = new RoomTypeImage
            {
                RecordNo = reader.GetInt32(reader.GetOrdinal("RecordNo")),
                NewID = reader.GetGuid(reader.GetOrdinal("NewID")),
                RoomTypeNewID = reader.GetGuid(reader.GetOrdinal("RoomTypeNewID")),
                ImagePath = reader.GetString(reader.GetOrdinal("ImagePath")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                DateCreated = reader.GetDateTime(reader.GetOrdinal("DateCreated")),
            };

            if (!result.TryGetValue(image.RoomTypeNewID, out var list))
            {
                list = new List<RoomTypeImage>();
                result[image.RoomTypeNewID] = list;
            }
            list.Add(image);
        }

        return result;
    }

    public async Task<int> CountForRoomTypeAsync(Guid roomTypeNewId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM Alban.tRoomTypeImage WHERE RoomTypeNewID = @RoomTypeNewID",
            connection);
        command.Parameters.AddWithValue("@RoomTypeNewID", roomTypeNewId);

        return (int)await command.ExecuteScalarAsync(cancellationToken);
    }

    public async Task InsertAsync(
        Guid roomTypeNewId,
        string imagePath,
        int sortOrder,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            INSERT INTO Alban.tRoomTypeImage (RoomTypeNewID, ImagePath, SortOrder, CreatedBy)
            VALUES (@RoomTypeNewID, @ImagePath, @SortOrder, @CreatedBy)
            """,
            connection);

        command.Parameters.AddWithValue("@RoomTypeNewID", roomTypeNewId);
        command.Parameters.AddWithValue("@ImagePath", imagePath);
        command.Parameters.AddWithValue("@SortOrder", sortOrder);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> DeleteAsync(Guid imageNewId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            DELETE FROM Alban.tRoomTypeImage
            OUTPUT DELETED.ImagePath
            WHERE NewID = @NewID
            """,
            connection);
        command.Parameters.AddWithValue("@NewID", imageNewId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }
}
