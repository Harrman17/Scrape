namespace AmazonScraper.Api.Models;

public class ScrapingJob
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int TotalAsins { get; set; }
    public int ProcessedAsins { get; set; }
    public int SuccessfulAsins { get; set; }
    public int BlockedAsins { get; set; }
    public bool JobComplete { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
