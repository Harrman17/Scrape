using Npgsql;
using AmazonScraper.Api.Models;

namespace AmazonScraper.Api.Services;

public class InventoryRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public InventoryRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<InventoryItem>> GetAllAsync()
    {
        const string sql = @"
            SELECT id, asin, title, image_url, amazon_url, amazon_price,
                   selling_price, in_stock, currency, is_active,
                   last_scraped, created_at, ebay_item_id
            FROM inventory
            ORDER BY created_at DESC";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var items = new List<InventoryItem>();
        while (await reader.ReadAsync())
        {
            items.Add(new InventoryItem
            {
                Id            = reader.GetInt64(0),
                Asin          = reader.GetString(1),
                Title         = reader.GetString(2),
                ImageUrl      = reader.IsDBNull(3) ? null : reader.GetString(3),
                AmazonUrl     = reader.GetString(4),
                AmazonPrice   = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                SellingPrice  = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                InStock       = reader.GetBoolean(7),
                Currency      = reader.IsDBNull(8) ? null : reader.GetString(8).Trim(),
                IsActive      = reader.GetBoolean(9),
                LastScraped   = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
                CreatedAt     = reader.IsDBNull(11) ? null : reader.GetFieldValue<DateTimeOffset>(11),
                EbayItemId    = reader.IsDBNull(12) ? null : reader.GetString(12),
            });
        }
        return items;
    }

    public async Task<InventoryItem> UpsertAsync(ScrapedProduct product)
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
                is_active    = true,
                last_scraped = NOW()
            RETURNING id, asin, title, image_url, amazon_url, amazon_price,
                      selling_price, stock_quantity, in_stock, currency, is_active,
                      last_scraped, created_at, ebay_item_id";

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

        return new InventoryItem
        {
            Id            = reader.GetInt64(0),
            Asin          = reader.GetString(1),
            Title         = reader.GetString(2),
            ImageUrl      = reader.IsDBNull(3) ? null : reader.GetString(3),
            AmazonUrl     = reader.GetString(4),
            AmazonPrice   = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
            SellingPrice  = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
            StockQuantity = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            InStock       = reader.GetBoolean(8),
            Currency      = reader.IsDBNull(9) ? null : reader.GetString(9).Trim(),
            IsActive      = reader.GetBoolean(10),
            LastScraped   = reader.IsDBNull(11) ? null : reader.GetFieldValue<DateTimeOffset>(11),
            CreatedAt     = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
            EbayItemId    = reader.IsDBNull(13) ? null : reader.GetString(13),
        };
    }
}
