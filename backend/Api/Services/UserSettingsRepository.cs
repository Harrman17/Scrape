using Npgsql;
using AmazonScraper.Api.Models;

namespace AmazonScraper.Api.Services;

/// <summary>
/// Repository for user settings (user_settings table).
/// Stores user-specific scraping and selling preferences.
/// </summary>
public class UserSettingsRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UserSettingsRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <summary>
    /// Get settings for a specific user.
    /// </summary>
    public async Task<UserSettings?> GetAsync(long userId)
    {
        const string sql = @"
            SELECT id, user_id, qty, profit_markup, block_products_under,
                   item_location_postcode, item_location_city, auto_remove_brand,
                   created_at, updated_at
            FROM user_settings
            WHERE user_id = @userId
            LIMIT 1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new UserSettings
        {
            Id = reader.GetInt64(0),
            UserId = reader.GetInt64(1),
            Qty = reader.GetInt32(2),
            ProfitMarkup = reader.GetDecimal(3),
            BlockProductsUnder = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            ItemLocationPostcode = reader.IsDBNull(5) ? null : reader.GetString(5),
            ItemLocationCity = reader.IsDBNull(6) ? null : reader.GetString(6),
            AutoRemoveBrand = reader.GetBoolean(7),
            CreatedAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            UpdatedAt = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
        };
    }

    /// <summary>
    /// Create default settings for a new user.
    /// </summary>
    public async Task<UserSettings> CreateAsync(long userId)
    {
        const string sql = @"
            INSERT INTO user_settings (user_id, qty, profit_markup, created_at, updated_at)
            VALUES (@userId, 1, 0, NOW(), NOW())
            RETURNING id, user_id, qty, profit_markup, block_products_under,
                      item_location_postcode, item_location_city, auto_remove_brand,
                      created_at, updated_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new UserSettings
        {
            Id = reader.GetInt64(0),
            UserId = reader.GetInt64(1),
            Qty = reader.GetInt32(2),
            ProfitMarkup = reader.GetDecimal(3),
            BlockProductsUnder = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            ItemLocationPostcode = reader.IsDBNull(5) ? null : reader.GetString(5),
            ItemLocationCity = reader.IsDBNull(6) ? null : reader.GetString(6),
            AutoRemoveBrand = reader.GetBoolean(7),
            CreatedAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            UpdatedAt = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
        };
    }

    /// <summary>
    /// Update user settings.
    /// </summary>
    public async Task<UserSettings> UpdateAsync(
        long userId,
        int qty,
        decimal profitMarkup,
        decimal? blockProductsUnder,
        string? itemLocationPostcode,
        string? itemLocationCity,
        bool autoRemoveBrand)
    {
        const string sql = @"
            UPDATE user_settings
            SET qty = @qty,
                profit_markup = @profitMarkup,
                block_products_under = @blockProductsUnder,
                item_location_postcode = @itemLocationPostcode,
                item_location_city = @itemLocationCity,
                auto_remove_brand = @autoRemoveBrand,
                updated_at = NOW()
            WHERE user_id = @userId
            RETURNING id, user_id, qty, profit_markup, block_products_under,
                      item_location_postcode, item_location_city, auto_remove_brand,
                      created_at, updated_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("qty", qty);
        cmd.Parameters.AddWithValue("profitMarkup", profitMarkup);
        cmd.Parameters.AddWithValue("blockProductsUnder", (object?)blockProductsUnder ?? DBNull.Value);
        cmd.Parameters.AddWithValue("itemLocationPostcode", (object?)itemLocationPostcode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("itemLocationCity", (object?)itemLocationCity ?? DBNull.Value);
        cmd.Parameters.AddWithValue("autoRemoveBrand", autoRemoveBrand);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new UserSettings
        {
            Id = reader.GetInt64(0),
            UserId = reader.GetInt64(1),
            Qty = reader.GetInt32(2),
            ProfitMarkup = reader.GetDecimal(3),
            BlockProductsUnder = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            ItemLocationPostcode = reader.IsDBNull(5) ? null : reader.GetString(5),
            ItemLocationCity = reader.IsDBNull(6) ? null : reader.GetString(6),
            AutoRemoveBrand = reader.GetBoolean(7),
            CreatedAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            UpdatedAt = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
        };
    }
}
