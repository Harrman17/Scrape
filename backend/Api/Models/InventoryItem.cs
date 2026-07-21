namespace AmazonScraper.Api.Models;

/// <summary>
/// Represents a product in the shared inventory catalogue.
/// This is the global product data, not user-specific.
/// </summary>
public class Inventory
{
    public long Id { get; set; }
    public string Asin { get; set; } = "";
    public string Title { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string AmazonUrl { get; set; } = "";
    public decimal? AmazonPrice { get; set; }
    public string? Currency { get; set; }
    public bool InStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastScraped { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}
