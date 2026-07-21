using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AmazonScraper.Api.Models;
using AmazonScraper.Api.Services;

namespace AmazonScraper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly UserInventoryRepository _userInventory;
    private readonly UserSettingsRepository _userSettings;

    public InventoryController(
        UserInventoryRepository userInventory,
        UserSettingsRepository userSettings)
    {
        _userInventory = userInventory;
        _userSettings = userSettings;
    }

    /// <summary>
    /// Get the current user's inventory items.
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
    /// Update a user's inventory item (quantity, status, eBay item ID).
    /// </summary>
    [HttpPut("{userInventoryId}")]
    public async Task<ActionResult<UserInventory>> UpdateInventoryItem(
        long userInventoryId,
        [FromBody] UpdateInventoryRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        var updated = await _userInventory.UpdateAsync(
            userInventoryId,
            request.Qty,
            request.Status,
            request.EbayItemId);

        return Ok(updated);
    }

    /// <summary>
    /// Delete/deactivate a user's inventory item.
    /// </summary>
    [HttpDelete("{userInventoryId}")]
    public async Task<IActionResult> DeleteInventoryItem(long userInventoryId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        await _userInventory.DeleteAsync(userInventoryId);
        return NoContent();
    }

    /// <summary>
    /// Get the current user's settings.
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<UserSettingsDto>> GetSettings()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        var settings = await _userSettings.GetAsync(userId.Value);
        if (settings == null)
            return NotFound(new { error = "User settings not found." });

        return Ok(new UserSettingsDto
        {
            Id = settings.Id,
            Qty = settings.Qty,
            ProfitMarkup = settings.ProfitMarkup,
            BlockProductsUnder = settings.BlockProductsUnder,
            ItemLocationPostcode = settings.ItemLocationPostcode,
            ItemLocationCity = settings.ItemLocationCity,
            AutoRemoveBrand = settings.AutoRemoveBrand,
        });
    }

    /// <summary>
    /// Update the current user's settings.
    /// </summary>
    [HttpPut("settings")]
    public async Task<ActionResult<UserSettingsDto>> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        var settings = await _userSettings.UpdateAsync(
            userId.Value,
            request.Qty,
            request.ProfitMarkup,
            request.BlockProductsUnder,
            request.ItemLocationPostcode,
            request.ItemLocationCity,
            request.AutoRemoveBrand);

        return Ok(new UserSettingsDto
        {
            Id = settings.Id,
            Qty = settings.Qty,
            ProfitMarkup = settings.ProfitMarkup,
            BlockProductsUnder = settings.BlockProductsUnder,
            ItemLocationPostcode = settings.ItemLocationPostcode,
            ItemLocationCity = settings.ItemLocationCity,
            AutoRemoveBrand = settings.AutoRemoveBrand,
        });
    }

    private long? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdClaim?.Value, out var userId))
            return userId;
        return null;
    }
}

public class UpdateInventoryRequest
{
    public int Qty { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? EbayItemId { get; set; }
}

public class UpdateUserSettingsRequest
{
    public int Qty { get; set; } = 1;
    public decimal ProfitMarkup { get; set; } = 0;
    public decimal? BlockProductsUnder { get; set; }
    public string? ItemLocationPostcode { get; set; }
    public string? ItemLocationCity { get; set; }
    public bool AutoRemoveBrand { get; set; } = false;
}
