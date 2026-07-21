namespace AmazonScraper.Api.Models;

/// <summary>
/// Stores user-specific scraping and selling preferences.
/// These are default settings applied when a user imports/scrapes products.
/// </summary>
public class UserSettings
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int Qty { get; set; } = 1;
    public decimal ProfitMarkup { get; set; } = 0m; // In percentage (e.g., 20 = 20%)
    public decimal? BlockProductsUnder { get; set; } // Block products under this Amazon price
    public string? ItemLocationPostcode { get; set; }
    public string? ItemLocationCity { get; set; }
    public bool AutoRemoveBrand { get; set; } = false;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
