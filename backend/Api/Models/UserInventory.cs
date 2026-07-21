namespace AmazonScraper.Api.Models;

/// <summary>
/// Represents the relationship between users and inventory items.
/// Stores user-specific ownership and listing information for products.
/// </summary>
public class UserInventory
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long InventoryId { get; set; }
    public int Qty { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, ACTIVE, DELISTED
    public string? EbayItemId { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
