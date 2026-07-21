using Npgsql;
using AmazonScraper.Api.Models;

namespace AmazonScraper.Api.Services;

public class UsersRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UsersRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<User?> FindByIdAsync(long id)
    {
        const string sql = @"
            SELECT id, name, email, password_hash, created_at, updated_at
            FROM users
            WHERE id = @id
            LIMIT 1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new User
        {
            Id           = reader.GetInt64(0),
            Name         = reader.GetString(1),
            Email        = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            CreatedAt    = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            UpdatedAt    = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
        };
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        const string sql = @"
            SELECT id, name, email, password_hash, created_at, updated_at
            FROM users
            WHERE LOWER(email) = LOWER(@email)
            LIMIT 1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("email", email.Trim());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new User
        {
            Id           = reader.GetInt64(0),
            Name         = reader.GetString(1),
            Email        = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            CreatedAt    = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            UpdatedAt    = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
        };
    }

    public async Task<User?> CreateAsync(string name, string email, string passwordHash)
    {
        const string sql = @"
            INSERT INTO users (name, email, password_hash, created_at, updated_at)
            VALUES (@name, @email, @passwordHash, NOW(), NOW())
            RETURNING id, name, email, password_hash, created_at, updated_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name", name.Trim());
        cmd.Parameters.AddWithValue("email", email.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("passwordHash", passwordHash);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new User
        {
            Id           = reader.GetInt64(0),
            Name         = reader.GetString(1),
            Email        = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            CreatedAt    = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            UpdatedAt    = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
        };
    }
}
