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

    [JsonPropertyName("image_urls")]
    public List<string> ImageUrls { get; set; } = new();

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("amazon_price")]
    public decimal? AmazonPrice { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "GBP";

    [JsonPropertyName("in_stock")]
    public bool InStock { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("mpn")]
    public string? Mpn { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("product_type")]
    public string? ProductType { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    [JsonPropertyName("ean")]
    public string? Ean { get; set; }

    [JsonPropertyName("upc")]
    public string? Upc { get; set; }

    [JsonPropertyName("isbn")]
    public string? Isbn { get; set; }

    [JsonPropertyName("height")]
    public string? Height { get; set; }

    [JsonPropertyName("width")]
    public string? Width { get; set; }

    [JsonPropertyName("length")]
    public string? Length { get; set; }

    [JsonPropertyName("weight")]
    public string? Weight { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
