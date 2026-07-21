using System.Text.Json.Serialization;

namespace AmazonScraper.Api.Models;

public class ScrapedProduct
{
    [JsonPropertyName("asin")]
    public string Asin { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("amazon_price")]
    public decimal? AmazonPrice { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "GBP";

    [JsonPropertyName("in_stock")]
    public bool InStock { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
