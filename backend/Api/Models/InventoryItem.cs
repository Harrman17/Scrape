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
    public List<string> ImageUrls { get; set; } = new();
    public string AmazonUrl { get; set; } = "";
    public decimal? AmazonPrice { get; set; }
    public string? Currency { get; set; }
    public bool InStock { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public List<string> Features { get; set; } = new();
    public string? Brand { get; set; }
    public string? Mpn { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? ProductType { get; set; }
    public string? Department { get; set; }
    public string? Ean { get; set; }
    public string? Upc { get; set; }
    public string? Isbn { get; set; }
    public string? Height { get; set; }
    public string? Width { get; set; }
    public string? Length { get; set; }
    public string? Weight { get; set; }
    public string? EbayCategory { get; set; }
    public string? EbayCategoryName { get; set; }
    public DateTimeOffset? LastScraped { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}
