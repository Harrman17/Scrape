using Npgsql;
using AmazonScraper.Api.Models;

namespace AmazonScraper.Api.Services;

/// <summary>
/// Repository for the shared Inventory catalogue.
/// Products here are shared across all users.
/// </summary>
public class InventoryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public InventoryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<Inventory>> GetAllAsync()
    {
        const string sql = @"
            SELECT id, asin, title, image_url, amazon_url, amazon_price,
                   currency, in_stock, is_active, description, ebay_category, ebay_category_name,
                   last_scraped, created_at
            FROM inventory
            ORDER BY created_at DESC";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var items = new List<Inventory>();
        while (await reader.ReadAsync())
        {
            items.Add(new Inventory
            {
                Id          = reader.GetInt64(0),
                Asin        = reader.GetString(1),
                Title       = reader.GetString(2),
                ImageUrl    = reader.IsDBNull(3) ? null : reader.GetString(3),
                AmazonUrl   = reader.GetString(4),
                AmazonPrice = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                Currency    = reader.IsDBNull(6) ? null : reader.GetString(6).Trim(),
                InStock     = reader.GetBoolean(7),
                IsActive    = reader.GetBoolean(8),
                Description       = reader.IsDBNull(9)  ? null : reader.GetString(9),
                EbayCategory      = reader.IsDBNull(10) ? null : reader.GetString(10),
                EbayCategoryName  = reader.IsDBNull(11) ? null : reader.GetString(11),
                LastScraped = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
                CreatedAt   = reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTimeOffset>(13),
            });
        }
        return items;
    }

    public async Task<Inventory?> GetByAsinAsync(string asin)
    {
        const string sql = @"
            SELECT id, asin, title, image_url, amazon_url, amazon_price,
                   currency, in_stock, is_active, description, ebay_category, ebay_category_name,
                   last_scraped, created_at
            FROM inventory
            WHERE asin = @asin
            LIMIT 1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("asin", asin.Trim());
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return null;

        return new Inventory
        {
            Id          = reader.GetInt64(0),
            Asin        = reader.GetString(1),
            Title       = reader.GetString(2),
            ImageUrl    = reader.IsDBNull(3) ? null : reader.GetString(3),
            AmazonUrl   = reader.GetString(4),
            AmazonPrice = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
            Currency    = reader.IsDBNull(6) ? null : reader.GetString(6).Trim(),
            InStock     = reader.GetBoolean(7),
            Description       = reader.IsDBNull(9)  ? null : reader.GetString(9),
            EbayCategory      = reader.IsDBNull(10) ? null : reader.GetString(10),
            EbayCategoryName  = reader.IsDBNull(11) ? null : reader.GetString(11),
            LastScraped = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
            CreatedAt   = reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTimeOffset>(13),
        };
    }

    public async Task<Inventory> UpsertAsync(ScrapedProduct product)
    {
        const string sql = @"
            INSERT INTO inventory (asin, title, image_url, amazon_url, amazon_price, currency, in_stock, is_active, description, last_scraped)
            VALUES (@asin, @title, @imageUrl, @amazonUrl, @amazonPrice, @currency, @inStock, true, @description, NOW())
            ON CONFLICT (asin)
            DO UPDATE SET
                title        = EXCLUDED.title,
                image_url    = EXCLUDED.image_url,
                amazon_url   = EXCLUDED.amazon_url,
                amazon_price = EXCLUDED.amazon_price,
                currency     = EXCLUDED.currency,
                in_stock     = EXCLUDED.in_stock,
                is_active    = EXCLUDED.is_active,
                description  = EXCLUDED.description,
                last_scraped = NOW()
            RETURNING id, asin, title, image_url, amazon_url, amazon_price,
                      currency, in_stock, is_active, description, ebay_category, ebay_category_name,
                      last_scraped, created_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("asin", product.Asin);
        cmd.Parameters.AddWithValue("title", product.Title);
        cmd.Parameters.AddWithValue("imageUrl", (object?)product.ImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("amazonUrl", product.Url);
        cmd.Parameters.AddWithValue("amazonPrice", (object?)product.AmazonPrice ?? DBNull.Value);
        cmd.Parameters.AddWithValue("currency", product.Currency);
        cmd.Parameters.AddWithValue("inStock", product.InStock);
        cmd.Parameters.AddWithValue("description", (object?)product.Description ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return new Inventory
        {
            Id          = reader.GetInt64(0),
            Asin        = reader.GetString(1),
            Title       = reader.GetString(2),
            ImageUrl    = reader.IsDBNull(3) ? null : reader.GetString(3),
            AmazonUrl   = reader.GetString(4),
            AmazonPrice = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
            Currency    = reader.IsDBNull(6) ? null : reader.GetString(6).Trim(),
            InStock     = reader.GetBoolean(7),
            IsActive    = reader.GetBoolean(8),
            Description       = reader.IsDBNull(9)  ? null : reader.GetString(9),
            EbayCategory      = reader.IsDBNull(10) ? null : reader.GetString(10),
            EbayCategoryName  = reader.IsDBNull(11) ? null : reader.GetString(11),
            LastScraped = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
            CreatedAt   = reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTimeOffset>(13),
        };
    }

    public async Task UpdateEbayCategoryAsync(long id, string categoryId, string categoryName)
    {
        const string sql = @"
            UPDATE inventory
            SET ebay_category = @categoryId, ebay_category_name = @categoryName
            WHERE id = @id";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("categoryId", categoryId);
        cmd.Parameters.AddWithValue("categoryName", categoryName);
        await cmd.ExecuteNonQueryAsync();
    }
}
