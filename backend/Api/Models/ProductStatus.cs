namespace AmazonScraper.Api.Models;

/// <summary>
/// Represents the status of a product in the user's inventory.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product has been scraped but not yet paired/listed on eBay. Color: Black
    /// This is the default status when a product is first added to inventory.
    /// </summary>
    Unpaired,

    /// <summary>
    /// Product is actively listed on eBay. Color: Green
    /// </summary>
    Active,

    /// <summary>
    /// Product listing has ended on eBay. Color: Grey
    /// </summary>
    EndedOnEbay,

    /// <summary>
    /// Product has issues (e.g., listing error, inventory mismatch). Color: Red
    /// </summary>
    Issues
}
