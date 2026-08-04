using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;
using System.Security.Claims;
using AmazonScraper.Api.Models;
using AmazonScraper.Api.Services;

namespace AmazonScraper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScrapeController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly InventoryRepository _inventory;
    private readonly UserInventoryRepository _userInventory;
    private readonly UserSettingsRepository _userSettings;
    private readonly ScrapingJobsRepository _scrapingJobs;
    private readonly EbayCategoryService _ebayCategory;

    public ScrapeController(
        IConfiguration config,
        InventoryRepository inventory,
        UserInventoryRepository userInventory,
        UserSettingsRepository userSettings,
        ScrapingJobsRepository scrapingJobs,
        EbayCategoryService ebayCategory)
    {
        _config = config;
        _inventory = inventory;
        _userInventory = userInventory;
        _userSettings = userSettings;
        _scrapingJobs = scrapingJobs;
        _ebayCategory = ebayCategory;
    }

    /// <summary>
    /// Get all products in the global inventory (for admin/reference).
    /// </summary>
    [HttpGet("inventory")]
    public async Task<ActionResult<List<Inventory>>> GetAllInventory()
    {
        var items = await _inventory.GetAllAsync();
        return Ok(items);
    }

    /// <summary>
    /// Get the current user's inventory with full details.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserInventoryDto>>> GetUserInventory()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        var items = await _userInventory.GetUserInventoryAsync(userId.Value);
        return Ok(items);
    }

    /// <summary>
    /// Scrape products and add them to the user's inventory.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> Post([FromBody] ScrapeRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        if (request.Asins is null || request.Asins.Count == 0)
            return BadRequest(new { error = "At least one ASIN is required." });

        var python = _config["Scraper:PythonExecutable"] ?? "python3";
        var script = _config["Scraper:ScriptPath"] ?? "";

        Console.WriteLine($"[Scrape] Starting scrape for ASINs: {string.Join(", ", request.Asins)}");
        Console.WriteLine($"[Scrape] Using Python: {python}");
        Console.WriteLine($"[Scrape] Using script: {script}");

        if (string.IsNullOrWhiteSpace(script))
            return StatusCode(500, new { error = "Scraper:ScriptPath is not configured in appsettings." });

        var asinArguments = string.Join(" ", request.Asins.Select(asin => $"\"{asin}\""));
        var startInfo = new ProcessStartInfo
        {
            FileName = python,
            Arguments = $"\"{script}\" {asinArguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        if (process is null)
            return StatusCode(500, new { error = "Failed to start Python scraper." });

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        Console.WriteLine($"[Scrape] Process exited with code: {process.ExitCode}");
        Console.WriteLine($"[Scrape] stdout: {stdout}");
        Console.WriteLine($"[Scrape] stderr: {stderr}");

        if (process.ExitCode != 0)
            return StatusCode(500, new { error = stderr.Trim(), stdout });

        var scraped = JsonSerializer.Deserialize<List<ScrapedProduct>>(stdout);
        if (scraped is null || scraped.Count == 0)
            return StatusCode(500, new { error = "No results returned from scraper." });

        Console.WriteLine($"[Scrape] Parsed {scraped.Count} products from scraper output");

        // Create a scraping job record
        var job = await _scrapingJobs.CreateAsync(userId.Value, scraped.Count);

        // Get user settings to use as defaults
        var settings = await _userSettings.GetAsync(userId.Value);
        if (settings == null)
        {
            settings = await _userSettings.CreateAsync(userId.Value);
        }

        // Get all ASINs the user already has in their inventory
        var existingAsins = await _userInventory.GetUserAsinsAsync(userId.Value);
        Console.WriteLine($"[Scrape] User has {existingAsins.Count} existing products in inventory");

        var saved = new List<UserInventoryDto>();
        var errors = new List<object>();
        var blocked = new List<object>();

        foreach (var product in scraped)
        {
            if (!string.IsNullOrWhiteSpace(product.Error))
            {
                Console.WriteLine($"[Scrape] Product {product.Asin} had scraper error: {product.Error}");
                errors.Add(new { product.Asin, product.Error });
                continue;
            }

            // Check if user already has this ASIN
            if (existingAsins.Contains(product.Asin))
            {
                Console.WriteLine($"[Scrape] Product {product.Asin} already exists in user inventory - blocking as duplicate");
                blocked.Add(new { 
                    Asin = product.Asin, 
                    Title = product.Title,
                    Reason = "Already in inventory" 
                });
                continue;
            }

            // Check if product price is below minimum threshold
            if (settings.BlockProductsUnder.HasValue && product.AmazonPrice.HasValue)
            {
                if (product.AmazonPrice.Value < settings.BlockProductsUnder.Value)
                {
                    Console.WriteLine($"[Scrape] Product {product.Asin} blocked: price £{product.AmazonPrice.Value} is below minimum £{settings.BlockProductsUnder.Value}");
                    blocked.Add(new {
                        Asin = product.Asin,
                        Title = product.Title,
                        Reason = $"Price £{product.AmazonPrice.Value:F2} is below minimum £{settings.BlockProductsUnder.Value:F2}"
                    });
                    continue;
                }
            }

            // Check against blocklist (both default and user's custom keywords)
            var allBlocklistKeywords = new List<string>(UserSettings.DefaultBlocklist);
            if (!string.IsNullOrWhiteSpace(settings.Blocklist))
            {
                var customKeywords = settings.Blocklist.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                allBlocklistKeywords.AddRange(customKeywords);
            }

            if (allBlocklistKeywords.Count > 0)
            {
                var textToCheck = $"{product.Title} {product.Description} {string.Join(" ", product.Features ?? new List<string>())}";
                var matchedKeyword = allBlocklistKeywords.FirstOrDefault(keyword => 
                    textToCheck.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                if (matchedKeyword != null)
                {
                    Console.WriteLine($"[Scrape] Product {product.Asin} blocked: matched blocklist keyword '{matchedKeyword}'");
                    blocked.Add(new {
                        Asin = product.Asin,
                        Title = product.Title,
                        Reason = $"Blocked by keyword: '{matchedKeyword}'"
                    });
                    continue;
                }
            }

            try
            {
                Console.WriteLine($"[Scrape] Suggesting category for product: {product.Asin} - {product.Title}");
                var (catId, catName) = await _ebayCategory.SuggestCategoryAsync(product.Title);
                if (string.IsNullOrWhiteSpace(catId) || string.IsNullOrWhiteSpace(catName))
                {
                    Console.WriteLine($"[Scrape] Category suggestion failed for {product.Asin}");
                    errors.Add(new { product.Asin, error = "Category suggestion failed or returned no category." });
                    continue;
                }

                Console.WriteLine($"[Scrape] Category for {product.Asin}: {catId} - {catName}");

                // Upsert into global inventory table
                Console.WriteLine($"[Scrape] Saving product to inventory: {product.Asin}");
                var inventoryItem = await _inventory.UpsertAsync(product);
                Console.WriteLine($"[Scrape] Inventory row created/updated with id: {inventoryItem.Id}");
                await _inventory.UpdateEbayCategoryAsync(inventoryItem.Id, catId, catName!);
                inventoryItem.EbayCategory = catId;
                inventoryItem.EbayCategoryName = catName;

                // Create new entry in user_inventory with default qty from settings
                Console.WriteLine($"[Scrape] Creating user inventory entry for user {userId.Value}, inventory {inventoryItem.Id}");
                var userInv = await _userInventory.CreateAsync(userId.Value, inventoryItem.Id, settings.Qty);

                // Build DTO with combined data
                var dto = new UserInventoryDto
                {
                    UserInventoryId = userInv.Id,
                    InventoryId = inventoryItem.Id,
                    Asin = inventoryItem.Asin,
                    Title = inventoryItem.Title,
                    ImageUrl = inventoryItem.ImageUrl,
                    ImageUrls = inventoryItem.ImageUrls,
                    AmazonUrl = inventoryItem.AmazonUrl,
                    AmazonPrice = inventoryItem.AmazonPrice,
                    Currency = inventoryItem.Currency,
                    InStock = inventoryItem.InStock,
                    LastScraped = inventoryItem.LastScraped,
                    Qty = userInv.Qty,
                    Status = userInv.Status,
                    EbayItemId = userInv.EbayItemId,
                    IsActive = inventoryItem.IsActive,
                    Description = inventoryItem.Description,
                    Features = inventoryItem.Features,
                    Brand = inventoryItem.Brand,
                    Mpn = inventoryItem.Mpn,
                    Model = inventoryItem.Model,
                    Color = inventoryItem.Color,
                    Size = inventoryItem.Size,
                    ProductType = inventoryItem.ProductType,
                    Department = inventoryItem.Department,
                    Ean = inventoryItem.Ean,
                    Upc = inventoryItem.Upc,
                    Isbn = inventoryItem.Isbn,
                    Height = inventoryItem.Height,
                    Width = inventoryItem.Width,
                    Length = inventoryItem.Length,
                    Weight = inventoryItem.Weight,
                    EbayCategory = inventoryItem.EbayCategory,
                    EbayCategoryName = inventoryItem.EbayCategoryName,
                    SellingPrice = CalculateSellingPrice(inventoryItem.AmazonPrice, settings.ProfitMarkup),
                };

                saved.Add(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Scrape] Exception for {product.Asin}: {ex}");
                errors.Add(new { product.Asin, error = ex.Message });
            }
        }

        // Mark job complete
        await _scrapingJobs.UpdateCompletedAsync(
            job.Id, 
            saved.Count + errors.Count + blocked.Count, 
            saved.Count, 
            blocked.Count
        );

        if (errors.Count > 0 && saved.Count == 0)
            return StatusCode(500, new { error = "All products failed to import.", errors, blocked });

        return Ok(new { saved, errors, blocked });
    }

    private long? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdClaim?.Value, out var userId))
            return userId;
        return null;
    }

    private decimal? CalculateSellingPrice(decimal? amazonPrice, decimal profitMarkup)
    {
        if (amazonPrice == null) return null;
        return amazonPrice * (1 + profitMarkup / 100);
    }

    /// <summary>
    /// Update prices and stock status for all user's inventory items.
    /// Returns a summary of changes detected.
    /// </summary>
    [HttpPost("update-prices")]
    public async Task<ActionResult<object>> UpdatePricesAndStock()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        var python = _config["Scraper:PythonExecutable"] ?? "python3";
        var script = _config["Scraper:PriceUpdateScriptPath"];

        if (string.IsNullOrWhiteSpace(script))
        {
            // Fallback to default path if not configured
            script = Path.Combine(
                Directory.GetParent(_config["Scraper:ScriptPath"] ?? "")?.FullName ?? "",
                "amzPriceStockUpdate.py"
            );
        }

        Console.WriteLine($"[PriceUpdate] Starting price update for user {userId}");
        Console.WriteLine($"[PriceUpdate] Using script: {script}");

        if (!System.IO.File.Exists(script))
            return StatusCode(500, new { error = $"Price update script not found: {script}" });

        // Get all inventory items for the user
        var userInventory = await _userInventory.GetUserInventoryAsync(userId.Value);
        if (userInventory.Count == 0)
            return Ok(new { message = "No inventory items to update", updated = 0, changes = new List<object>() });

        var asins = userInventory.Select(i => i.Asin).Distinct().ToList();
        Console.WriteLine($"[PriceUpdate] Checking {asins.Count} products");

        // Run the price update script
        var asinArguments = string.Join(" ", asins.Select(asin => $"\"{asin}\""));
        var startInfo = new ProcessStartInfo
        {
            FileName = python,
            Arguments = $"\"{script}\" {asinArguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        if (process is null)
            return StatusCode(500, new { error = "Failed to start price update script." });

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        Console.WriteLine($"[PriceUpdate] Process exited with code: {process.ExitCode}");
        
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Console.WriteLine($"[PriceUpdate] stderr: {stderr}");
        }

        if (process.ExitCode != 0)
        {
            return StatusCode(500, new { error = "Price update script failed", details = stderr });
        }

        Console.WriteLine($"[PriceUpdate] Script output: {stdout}");

        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        var updates = JsonSerializer.Deserialize<List<PriceStockUpdate>>(stdout, options);
        if (updates is null || updates.Count == 0)
            return StatusCode(500, new { error = "No results returned from price update script." });

        Console.WriteLine($"[PriceUpdate] Received {updates.Count} updates");
        Console.WriteLine($"[PriceUpdate] User has {userInventory.Count} inventory items");
        foreach (var item in userInventory)
        {
            Console.WriteLine($"[PriceUpdate] User inventory item: ASIN={item.Asin}, Price={item.AmazonPrice}, Stock={item.InStock}");
        }
        foreach (var upd in updates)
        {
            Console.WriteLine($"[PriceUpdate] Update received: ASIN={upd.Asin}, Price={upd.Price}, Stock={upd.InStock}, Error={upd.Error}");
        }

        // Process updates and detect changes
        var changes = new List<object>();
        var updatedCount = 0;
        var errorCount = 0;

        foreach (var update in updates)
        {
            if (!string.IsNullOrWhiteSpace(update.Error))
            {
                Console.WriteLine($"[PriceUpdate] Error for {update.Asin}: {update.Error}");
                errorCount++;
                continue;
            }

            // Find the current item in user's inventory
            var currentItem = userInventory.FirstOrDefault(i => i.Asin == update.Asin);
            if (currentItem == null)
            {
                Console.WriteLine($"[PriceUpdate] ASIN {update.Asin} not found in user inventory");
                continue;
            }

            var priceChanged = currentItem.AmazonPrice != update.Price;
            var stockChanged = currentItem.InStock != update.InStock;

            Console.WriteLine($"[PriceUpdate] Comparing {update.Asin}: DB price={currentItem.AmazonPrice}, New price={update.Price}, Changed={priceChanged}");
            Console.WriteLine($"[PriceUpdate] Stock {update.Asin}: DB stock={currentItem.InStock}, New stock={update.InStock}, Changed={stockChanged}");

            if (priceChanged || stockChanged)
            {
                // Update in database
                await _inventory.UpdatePriceAndStockAsync(update.Asin, update.Price, update.InStock);
                
                changes.Add(new
                {
                    asin = update.Asin,
                    title = currentItem.Title,
                    oldPrice = currentItem.AmazonPrice,
                    newPrice = update.Price,
                    priceChanged,
                    oldStock = currentItem.InStock,
                    newStock = update.InStock,
                    stockChanged
                });

                updatedCount++;
                Console.WriteLine($"[PriceUpdate] Updated {update.Asin}: Price {currentItem.AmazonPrice} → {update.Price}, Stock {currentItem.InStock} → {update.InStock}");
            }
            else
            {
                Console.WriteLine($"[PriceUpdate] No changes for {update.Asin}");
            }
        }

        Console.WriteLine($"[PriceUpdate] Complete: {updatedCount} changes, {errorCount} errors");

        return Ok(new
        {
            totalChecked = asins.Count,
            updated = updatedCount,
            errors = errorCount,
            changes
        });
    }

    /// <summary>
    /// Generate and download a CSV file for updating eBay listings with new prices and stock.
    /// </summary>
    [HttpGet("ebay-update-csv")]
    public async Task<IActionResult> DownloadEbayUpdateCsv()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        var userInventory = await _userInventory.GetUserInventoryAsync(userId.Value);
        
        // Filter for items that have eBay item IDs (already paired)
        var pairedItems = userInventory.Where(i => !string.IsNullOrWhiteSpace(i.EbayItemId)).ToList();

        if (pairedItems.Count == 0)
            return NotFound(new { error = "No paired eBay items found in inventory." });

        // Get user settings for profit markup
        var settings = await _userSettings.GetAsync(userId.Value);
        var profitMarkup = settings?.ProfitMarkup ?? 0m;

        Console.WriteLine($"[EbayCsv] Generating CSV for {pairedItems.Count} items");

        // Generate CSV content
        var csv = new System.Text.StringBuilder();
        
        // CSV Header for eBay File Exchange format - matches dilato format exactly
        csv.AppendLine("Action(SiteID=UK|Country=GB|Currency=GBP|Version=585|CC=UTF-8),ItemID,SiteID,Currency,StartPrice,BuyItNowPrice,Quantity,Relationship,RelationshipDetails,CustomLabel");

        foreach (var item in pairedItems)
        {
            var newPrice = CalculateSellingPrice(item.AmazonPrice, profitMarkup);
            var quantity = item.InStock ? item.Qty : 0;

            // Format: Revise,ItemID,UK,GBP,Price,,Quantity,,,ASIN
            csv.AppendLine($"Revise,{item.EbayItemId},UK,GBP,{newPrice:F2},,{quantity},,,{item.Asin}");
        }

        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"ebay-price-stock-update-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        Console.WriteLine($"[EbayCsv] CSV generated: {fileName}");

        return File(csvBytes, "text/csv", fileName);
    }
}

public class ScrapeRequest
{
    public List<string> Asins { get; set; } = new();
}

public class PriceStockUpdate
{
    public string Asin { get; set; } = "";
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "GBP";
    public bool InStock { get; set; }
    public string? Error { get; set; }
}
