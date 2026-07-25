using Npgsql;
using AmazonScraper.Api.Models;

namespace AmazonScraper.Api.Services;

public class ScrapingJobsRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ScrapingJobsRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<ScrapingJob>> GetByUserAsync(long userId)
    {
        const string sql = @"
            SELECT id, user_id, total_asins, processed_asins, successful_asins,
                   blocked_asins, job_complete, created_at, completed_at
            FROM scraping_jobs
            WHERE user_id = @userId
            ORDER BY created_at DESC";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync();

        var jobs = new List<ScrapingJob>();
        while (await reader.ReadAsync())
        {
            jobs.Add(new ScrapingJob
            {
                Id              = reader.GetInt64(0),
                UserId          = reader.GetInt64(1),
                TotalAsins      = reader.GetInt32(2),
                ProcessedAsins  = reader.GetInt32(3),
                SuccessfulAsins = reader.GetInt32(4),
                BlockedAsins    = reader.GetInt32(5),
                JobComplete     = reader.GetBoolean(6),
                CreatedAt       = reader.GetFieldValue<DateTimeOffset>(7),
                CompletedAt     = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            });
        }

        return jobs;
    }

    public async Task UpdateCompletedAsync(long jobId, int processedAsins, int successfulAsins, int blockedAsins)
    {
        const string sql = @"
            UPDATE scraping_jobs
            SET processed_asins  = @processedAsins,
                successful_asins = @successfulAsins,
                blocked_asins    = @blockedAsins,
                job_complete     = true,
                completed_at     = now()
            WHERE id = @jobId";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("jobId",          jobId);
        cmd.Parameters.AddWithValue("processedAsins", processedAsins);
        cmd.Parameters.AddWithValue("successfulAsins",successfulAsins);
        cmd.Parameters.AddWithValue("blockedAsins",   blockedAsins);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<ScrapingJob> CreateAsync(long userId, int totalAsins)
    {
        const string sql = @"
            INSERT INTO scraping_jobs (user_id, total_asins)
            VALUES (@userId, @totalAsins)
            RETURNING id, user_id, total_asins, processed_asins, successful_asins,
                      blocked_asins, job_complete, created_at, completed_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("totalAsins", totalAsins);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new ScrapingJob
        {
            Id              = reader.GetInt64(0),
            UserId          = reader.GetInt64(1),
            TotalAsins      = reader.GetInt32(2),
            ProcessedAsins  = reader.GetInt32(3),
            SuccessfulAsins = reader.GetInt32(4),
            BlockedAsins    = reader.GetInt32(5),
            JobComplete     = reader.GetBoolean(6),
            CreatedAt       = reader.GetFieldValue<DateTimeOffset>(7),
            CompletedAt     = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
        };
    }
}
