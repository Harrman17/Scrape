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
                   currency, in_stock, is_active, last_scraped, created_at
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
                LastScraped = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
                CreatedAt   = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
            });
        }
        return items;
    }

    public async Task<Inventory?> GetByAsinAsync(string asin)
    {
        const string sql = @"
            SELECT id, asin, title, image_url, amazon_url, amazon_price,
                   currency, in_stock, is_active, last_scraped, created_at
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
            LastScraped = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            CreatedAt   = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
        };
    }

    public async Task<Inventory> UpsertAsync(ScrapedProduct product)
    {
        const string sql = @"
            INSERT INTO inventory (asin, title, image_url, amazon_url, amazon_price, currency, in_stock, is_active, last_scraped)
            VALUES (@asin, @title, @imageUrl, @amazonUrl, @amazonPrice, @currency, @inStock, true, NOW())
            ON CONFLICT (asin)
            DO UPDATE SET
                title        = EXCLUDED.title,
                image_url    = EXCLUDED.image_url,
                amazon_url   = EXCLUDED.amazon_url,
                amazon_price = EXCLUDED.amazon_price,
                currency     = EXCLUDED.currency,
                in_stock     = EXCLUDED.in_stock,
                is_active    = EXCLUDED.is_active,
                last_scraped = NOW()
            RETURNING id, asin, title, image_url, amazon_url, amazon_price,
                      currency, in_stock, is_active, last_scraped, created_at";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("asin", product.Asin);
        cmd.Parameters.AddWithValue("title", product.Title);
        cmd.Parameters.AddWithValue("imageUrl", (object?)product.ImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("amazonUrl", product.Url);
        cmd.Parameters.AddWithValue("amazonPrice", (object?)product.AmazonPrice ?? DBNull.Value);
        cmd.Parameters.AddWithValue("currency", product.Currency);
        cmd.Parameters.AddWithValue("inStock", product.InStock);

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
            LastScraped = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
            CreatedAt   = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
        };
    }
}
