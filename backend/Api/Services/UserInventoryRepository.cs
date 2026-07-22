using Npgsql;
using AmazonScraper.Api.Models;

namespace AmazonScraper.Api.Services;

/// <summary>
/// Repository for user-specific inventory (user_inventory table).
/// Manages the relationship between users and products.
/// </summary>
public class UserInventoryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UserInventoryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <summary>
    /// Get all inventory items for a specific user with full details.
    /// </summary>
    public async Task<List<UserInventoryDto>> GetUserInventoryAsync(long userId)
    {
        const string sql = @"
            SELECT 
                ui.id as user_inventory_id,
                i.id as inventory_id,
                i.asin,
                i.title,
                i.image_url,
                i.amazon_url,
                i.amazon_price,
                i.currency,
                i.in_stock,
                i.is_active,
                i.last_scraped,
                ui.qty,
                ui.status,
                ui.ebay_item_id,
                us.profit_markup
            FROM user_inventory ui
            INNER JOIN inventory i ON ui.inventory_id = i.id
            INNER JOIN user_settings us ON us.user_id = ui.user_id
            WHERE ui.user_id = @userId
            ORDER BY ui.created_at DESC";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync();

        var items = new List<UserInventoryDto>();
        while (await reader.ReadAsync())
        {
            var amazonPrice = reader.IsDBNull(6) ? (decimal?)null : reader.GetDecimal(6);
            var profitMarkup = reader.GetDecimal(14);
            
            // Calculate selling price: AmazonPrice × (1 + ProfitMarkup/100)
            decimal? sellingPrice = null;
            if (amazonPrice.HasValue)
            {
                sellingPrice = Math.Round(amazonPrice.Value * (1 + profitMarkup / 100), 2);
            }
            
            items.Add(new UserInventoryDto
            {
                UserInventoryId = reader.GetInt64(0),
                InventoryId = reader.GetInt64(1),
                Asin = reader.GetString(2),
                Title = reader.GetString(3),
                ImageUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                AmazonUrl = reader.GetString(5),
                AmazonPrice = amazonPrice,
                Currency = reader.IsDBNull(7) ? null : reader.GetString(7).Trim(),
                InStock = reader.GetBoolean(8),
                IsActive = reader.GetBoolean(9),
                LastScraped = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
                Qty = reader.GetInt32(11),
                Status = reader.GetString(12),
                EbayItemId = reader.IsDBNull(13) ? null : reader.GetString(13),
                SellingPrice = sellingPrice,
            });
        }
        return items;
    }

    /// <summary>
    /// Check if a user already has an inventory item.
    /// </summary>
    public async Task<UserInventory?> GetAsync(long userId, long inventoryId)
    {
        const string sql = @"
            SELECT id, user_id, inventory_id, qty, status, ebay_item_id, created_at, updated_at
            FROM user_inventory
            WHERE user_id = @userId AND inventory_id = @inventoryId
            LIMIT 1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("inventoryId", inventoryId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new UserInventory
        {
            Id = reader.GetInt64(0),
            UserId = reader.GetInt64(1),
            InventoryId = reader.GetInt64(2),
            Qty = reader.GetInt32(3),
            Status = reader.GetString(4),
            EbayItemId = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            UpdatedAt = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
        };
    }

    /// <summary>
    /// Create a new user inventory entry with default settings from user_settings.
    /// </summary>
    public async Task<UserInventory> CreateAsync(long userId, long inventoryId, int qty)
    {
        const string sql = @"
            INSERT INTO user_inventory (user_id, inventory_id, qty, status, created_at, updated_at)
            VALUES (@userId, @inventoryId, @qty, 'PENDING', NOW(), NOW())
            RETURNING id, user_id, inventory_id, qty, status, ebay_item_id, created_at, updated_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("inventoryId", inventoryId);
        cmd.Parameters.AddWithValue("qty", qty);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new UserInventory
        {
            Id = reader.GetInt64(0),
            UserId = reader.GetInt64(1),
            InventoryId = reader.GetInt64(2),
            Qty = reader.GetInt32(3),
            Status = reader.GetString(4),
            EbayItemId = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            UpdatedAt = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
        };
    }

    /// <summary>
    /// Update an existing user inventory entry.
    /// </summary>
    public async Task<UserInventory> UpdateAsync(long id, int qty, string status, string? ebayItemId)
    {
        const string sql = @"
            UPDATE user_inventory
            SET qty = @qty, status = @status, ebay_item_id = @ebayItemId, updated_at = NOW()
            WHERE id = @id
            RETURNING id, user_id, inventory_id, qty, status, ebay_item_id, created_at, updated_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("qty", qty);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("ebayItemId", (object?)ebayItemId ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new UserInventory
        {
            Id = reader.GetInt64(0),
            UserId = reader.GetInt64(1),
            InventoryId = reader.GetInt64(2),
            Qty = reader.GetInt32(3),
            Status = reader.GetString(4),
            EbayItemId = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            UpdatedAt = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
        };
    }

    /// <summary>
    /// Delete a user inventory entry.
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        const string sql = @"
            DELETE FROM user_inventory
            WHERE id = @id";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Update all inventory quantities for a user (applies qty setting to existing items).
    /// </summary>
    public async Task UpdateAllQuantitiesAsync(long userId, int newQty)
    {
        const string sql = @"
            UPDATE user_inventory
            SET qty = @qty, updated_at = NOW()
            WHERE user_id = @userId";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("qty", newQty);
        await cmd.ExecuteNonQueryAsync();
    }
}
