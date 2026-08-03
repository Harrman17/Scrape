namespace AmazonScraper.Api.Models;

/// <summary>
/// DTO for API responses combining Inventory and UserInventory data.
/// This is what gets sent to the frontend when fetching user's products.
/// </summary>
public class UserInventoryDto
{
    public long UserInventoryId { get; set; }
    public long InventoryId { get; set; }
    
    // Inventory data
    public string Asin { get; set; } = "";
    public string Title { get; set; } = "";
    public string? ImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string AmazonUrl { get; set; } = "";
    public decimal? AmazonPrice { get; set; }
    public string? Currency { get; set; }
    public bool InStock { get; set; }
    public bool IsActive { get; set; }
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
    
    // User-specific data
    public int Qty { get; set; }
    public ProductStatus Status { get; set; }
    public string? EbayItemId { get; set; }
    
    // Calculated field
    public decimal? SellingPrice { get; set; } // Calculated based on AmazonPrice + markup
}

/// <summary>
/// DTO for displaying default user settings.
/// </summary>
public class UserSettingsDto
{
    public long Id { get; set; }
    public int Qty { get; set; }
    public decimal ProfitMarkup { get; set; }
    public decimal? BlockProductsUnder { get; set; }
    public string? ItemLocationPostcode { get; set; }
    public string? ItemLocationCity { get; set; }
    public bool AutoRemoveBrand { get; set; }
    public string? Blocklist { get; set; }
    public List<string> DefaultBlocklist { get; set; } = UserSettings.DefaultBlocklist;
}
