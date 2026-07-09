using Microsoft.Data.SqlClient;

namespace AlbansLodgingHouse.Data;

public class ManagementAccessService
{
    private readonly string _connectionString;

    public ManagementAccessService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AlbansDb")
            ?? throw new InvalidOperationException("Connection string 'AlbansDb' is not configured.");
    }

    public async Task<bool> IsHostAllowedAsync(string hostName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostName)) return false;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT 1 FROM Alban.tManagementAccess WHERE HostName = @HostName AND IsActive = 1",
            connection);
        command.Parameters.AddWithValue("@HostName", hostName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }
}
