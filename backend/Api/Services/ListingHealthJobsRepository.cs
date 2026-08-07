using Npgsql;
using AmazonScraper.Api.Models;

namespace AmazonScraper.Api.Services;

public class ListingHealthJobsRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ListingHealthJobsRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <summary>
    /// Get all listing health jobs for a specific user, ordered by most recent first.
    /// </summary>
    public async Task<List<ListingHealthJob>> GetByUserAsync(long userId)
    {
        const string sql = @"
            SELECT id, user_id, status, processed_items, healthy_items,
                   error_items, created_at, started_at
            FROM listing_health_jobs
            WHERE user_id = @userId
            ORDER BY created_at DESC";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync();

        var jobs = new List<ListingHealthJob>();
        while (await reader.ReadAsync())
        {
            jobs.Add(new ListingHealthJob
            {
                Id              = reader.GetInt64(0),
                UserId          = reader.GetInt64(1),
                Status          = reader.GetString(2),
                ProcessedItems  = reader.GetInt32(3),
                HealthyItems    = reader.GetInt32(4),
                ErrorItems      = reader.GetInt32(5),
                CreatedAt       = reader.GetFieldValue<DateTimeOffset>(6),
                StartedAt       = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
            });
        }

        return jobs;
    }

    /// <summary>
    /// Create a new listing health job for a user.
    /// </summary>
    public async Task<ListingHealthJob> CreateAsync(long userId)
    {
        const string sql = @"
            INSERT INTO listing_health_jobs (user_id, status)
            VALUES (@userId, 'pending')
            RETURNING id, user_id, status, processed_items, healthy_items,
                      error_items, created_at, started_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new ListingHealthJob
        {
            Id             = reader.GetInt64(0),
            UserId         = reader.GetInt64(1),
            Status         = reader.GetString(2),
            ProcessedItems = reader.GetInt32(3),
            HealthyItems   = reader.GetInt32(4),
            ErrorItems     = reader.GetInt32(5),
            CreatedAt      = reader.GetFieldValue<DateTimeOffset>(6),
            StartedAt      = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
        };
    }

    /// <summary>
    /// Mark a listing health job as started.
    /// </summary>
    public async Task UpdateStartedAsync(long jobId)
    {
        const string sql = @"
            UPDATE listing_health_jobs
            SET status = 'processing',
                started_at = now()
            WHERE id = @jobId";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("jobId", jobId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Update job statistics during processing.
    /// </summary>
    public async Task UpdateStatsAsync(long jobId, int processedItems, int healthyItems, int errorItems)
    {
        const string sql = @"
            UPDATE listing_health_jobs
            SET processed_items = @processedItems,
                healthy_items   = @healthyItems,
                error_items     = @errorItems
            WHERE id = @jobId";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("jobId",          jobId);
        cmd.Parameters.AddWithValue("processedItems", processedItems);
        cmd.Parameters.AddWithValue("healthyItems",   healthyItems);
        cmd.Parameters.AddWithValue("errorItems",     errorItems);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Mark a listing health job as complete.
    /// </summary>
    public async Task UpdateCompletedAsync(long jobId, int processedItems, int healthyItems, int errorItems)
    {
        const string sql = @"
            UPDATE listing_health_jobs
            SET status          = 'completed',
                processed_items = @processedItems,
                healthy_items   = @healthyItems,
                error_items     = @errorItems
            WHERE id = @jobId";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("jobId",          jobId);
        cmd.Parameters.AddWithValue("processedItems", processedItems);
        cmd.Parameters.AddWithValue("healthyItems",   healthyItems);
        cmd.Parameters.AddWithValue("errorItems",     errorItems);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Mark a listing health job as failed.
    /// </summary>
    public async Task UpdateFailedAsync(long jobId, string errorMessage)
    {
        const string sql = @"
            UPDATE listing_health_jobs
            SET status = 'failed'
            WHERE id = @jobId";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("jobId", jobId);
        await cmd.ExecuteNonQueryAsync();
    }
}
