using Microsoft.Data.SqlClient;

namespace AlbansLodgingHouse.Data;

public record ManagementEmailAddress(string Email, string? FullName);

public class ManagementEmailService
{
    private readonly string _connectionString;

    public ManagementEmailService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AlbansDb")
            ?? throw new InvalidOperationException("Connection string 'AlbansDb' is not configured.");
    }

    public async Task<IReadOnlyList<ManagementEmailAddress>> GetActiveEmailsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT Email, FullName FROM Alban.tManagementEmail WHERE IsActive = 1",
            connection);

        var results = new List<ManagementEmailAddress>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var email = reader.GetString(0);
            var fullName = reader.IsDBNull(1) ? null : reader.GetString(1);
            results.Add(new ManagementEmailAddress(email, fullName));
        }

        return results;
    }
}
