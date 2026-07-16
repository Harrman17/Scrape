namespace AmazonScraper.Api.Models;

public class InventoryItem
{
    public long Id { get; set; }
    public string Asin { get; set; } = "";
    public string Title { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string AmazonUrl { get; set; } = "";
    public decimal? AmazonPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public int? StockQuantity { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastScraped { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}
