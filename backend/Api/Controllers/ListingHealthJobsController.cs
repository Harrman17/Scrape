using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Globalization;
using AmazonScraper.Api.Models;
using AmazonScraper.Api.Services;

namespace AmazonScraper.Api.Controllers;

[ApiController]
[Route("api/listing-health-jobs")]
[Authorize]
public class ListingHealthJobsController : ControllerBase
{
    private readonly ListingHealthJobsRepository _jobs;
    private readonly UserInventoryRepository _userInventory;

    public ListingHealthJobsController(
        ListingHealthJobsRepository jobs,
        UserInventoryRepository userInventory)
    {
        _jobs = jobs;
        _userInventory = userInventory;
    }

    /// <summary>
    /// Get all listing health jobs for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ListingHealthJob>>> GetJobs()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        var jobs = await _jobs.GetByUserAsync(userId.Value);
        return Ok(jobs);
    }

    /// <summary>
    /// Upload and process an eBay CSV file to update listing health.
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<ListingHealthJob>> UploadCsv(IFormFile file)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "File must be a CSV." });

        // Create job record
        var job = await _jobs.CreateAsync(userId.Value);
        
        try
        {
            await _jobs.UpdateStartedAsync(job.Id);
            
            var (processedItems, healthyItems, errorItems) = await ProcessEbayCsvAsync(userId.Value, file);
            
            await _jobs.UpdateCompletedAsync(job.Id, processedItems, healthyItems, errorItems);
            
            // Return updated job
            var jobs = await _jobs.GetByUserAsync(userId.Value);
            var updatedJob = jobs.FirstOrDefault(j => j.Id == job.Id);
            return Ok(updatedJob ?? job);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListingHealth] Error processing CSV for job {job.Id}: {ex.Message}");
            await _jobs.UpdateFailedAsync(job.Id, ex.Message);
            return StatusCode(500, new { error = $"Failed to process CSV: {ex.Message}" });
        }
    }

    /// <summary>
    /// Process the eBay CSV and update inventory statuses.
    /// </summary>
    private async Task<(int processed, int healthy, int errors)> ProcessEbayCsvAsync(long userId, IFormFile file)
    {
        var processed = 0;
        var healthy = 0;
        var errors = 0;

        using var reader = new StreamReader(file.OpenReadStream());
        
        // Read header line
        var header = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(header))
            throw new Exception("CSV file is empty");

        var headers = ParseCsvLine(header);
        var itemIdIndex = Array.IndexOf(headers, "ItemID");
        var statusIndex = Array.IndexOf(headers, "Status");
        var customLabelIndex = Array.IndexOf(headers, "CustomLabel");

        if (itemIdIndex == -1 || statusIndex == -1 || customLabelIndex == -1)
            throw new Exception("CSV is missing required columns: ItemID, Status, or CustomLabel");

        Console.WriteLine($"[ListingHealth] Processing CSV for user {userId}");

        // Read data lines
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = ParseCsvLine(line);
            if (values.Length <= Math.Max(itemIdIndex, Math.Max(statusIndex, customLabelIndex)))
                continue;

            var ebayItemId = values[itemIdIndex];
            var status = values[statusIndex];
            var customLabel = values[customLabelIndex]; // ASIN

            if (string.IsNullOrWhiteSpace(ebayItemId) || string.IsNullOrWhiteSpace(customLabel))
                continue;

            processed++;

            try
            {
                // Determine inventory status based on eBay status
                string inventoryStatus;
                if (status == "Failure")
                {
                    inventoryStatus = "Issues";
                    errors++;
                }
                else if (status == "Warning" || status == "Success")
                {
                    inventoryStatus = "Active";
                    healthy++;
                }
                else
                {
                    // Unknown status, skip
                    continue;
                }

                // Update inventory by ASIN (CustomLabel)
                await _userInventory.UpdateStatusByAsinAsync(userId, customLabel, ebayItemId, inventoryStatus);
                
                Console.WriteLine($"[ListingHealth] Updated {customLabel} → {inventoryStatus} (eBay: {ebayItemId})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ListingHealth] Failed to update {customLabel}: {ex.Message}");
            }
        }

        Console.WriteLine($"[ListingHealth] Completed: {processed} processed, {healthy} healthy, {errors} errors");

        return (processed, healthy, errors);
    }

    /// <summary>
    /// Parse a CSV line handling quoted fields.
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString());
        return fields.ToArray();
    }

    private long? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return long.TryParse(claim, out var id) ? id : null;
    }
}
