namespace AmazonScraper.Api.Models;

/// <summary>
/// Represents a listing health check job where users upload an eBay CSV
/// to update inventory statuses based on current eBay listing data.
/// </summary>
public class ListingHealthJob
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public int ProcessedItems { get; set; }
    public int HealthyItems { get; set; }
    public int ErrorItems { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
}
