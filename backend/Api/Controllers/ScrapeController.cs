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

    public ScrapeController(
        IConfiguration config,
        InventoryRepository inventory,
        UserInventoryRepository userInventory,
        UserSettingsRepository userSettings)
    {
        _config = config;
        _inventory = inventory;
        _userInventory = userInventory;
        _userSettings = userSettings;
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

        if (process.ExitCode != 0)
            return StatusCode(500, new { error = stderr.Trim(), stdout });

        var scraped = JsonSerializer.Deserialize<List<ScrapedProduct>>(stdout);
        if (scraped is null || scraped.Count == 0)
            return StatusCode(500, new { error = "No results returned from scraper." });

        // Get user settings to use as defaults
        var settings = await _userSettings.GetAsync(userId.Value);
        if (settings == null)
        {
            settings = await _userSettings.CreateAsync(userId.Value);
        }

        var saved = new List<UserInventoryDto>();
        var errors = new List<object>();

        foreach (var product in scraped)
        {
            if (!string.IsNullOrWhiteSpace(product.Error))
            {
                errors.Add(new { product.Asin, product.Error });
                continue;
            }

            try
            {
                // Upsert into global inventory table
                var inventoryItem = await _inventory.UpsertAsync(product);

                // Check if user already has this product
                var existing = await _userInventory.GetAsync(userId.Value, inventoryItem.Id);

                UserInventory userInv;
                if (existing == null)
                {
                    // Create new entry in user_inventory with default qty from settings
                    userInv = await _userInventory.CreateAsync(userId.Value, inventoryItem.Id, settings.Qty);
                }
                else
                {
                    // User already has this product - just update it
                    userInv = existing;
                }

                // Build DTO with combined data
                var dto = new UserInventoryDto
                {
                    UserInventoryId = userInv.Id,
                    InventoryId = inventoryItem.Id,
                    Asin = inventoryItem.Asin,
                    Title = inventoryItem.Title,
                    ImageUrl = inventoryItem.ImageUrl,
                    AmazonUrl = inventoryItem.AmazonUrl,
                    AmazonPrice = inventoryItem.AmazonPrice,
                    Currency = inventoryItem.Currency,
                    InStock = inventoryItem.InStock,
                    LastScraped = inventoryItem.LastScraped,
                    Qty = userInv.Qty,
                    Status = userInv.Status,
                    EbayItemId = userInv.EbayItemId,
                    IsActive = inventoryItem.IsActive,
                    SellingPrice = CalculateSellingPrice(inventoryItem.AmazonPrice, settings.ProfitMarkup),
                };

                saved.Add(dto);
            }
            catch (Exception ex)
            {
                errors.Add(new { product.Asin, error = ex.Message });
            }
        }

        if (errors.Count > 0 && saved.Count == 0)
            return StatusCode(500, new { error = "All products failed to import.", errors });

        return Ok(new { saved, errors });
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
}

public class ScrapeRequest
{
    public List<string> Asins { get; set; } = new();
}
