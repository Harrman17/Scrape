using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using AmazonScraper.Api.Models;
using AmazonScraper.Api.Services;

namespace AmazonScraper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScrapeController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly InventoryRepository _inventory;

    public ScrapeController(IConfiguration config, InventoryRepository inventory)
    {
        _config = config;
        _inventory = inventory;
    }

    [HttpPost]
    public async Task<ActionResult<List<InventoryItem>>> Post([FromBody] ScrapeRequest request)
    {
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

        var saved = new List<InventoryItem>();
        var errors = new List<object>();

        foreach (var product in scraped)
        {
            if (!string.IsNullOrWhiteSpace(product.Error))
            {
                errors.Add(new { product.Asin, product.Error });
                continue;
            }

            var item = await _inventory.UpsertAsync(product);
            saved.Add(item);
        }

        if (errors.Count > 0 && saved.Count == 0)
            return StatusCode(500, new { error = "All products failed to scrape.", errors });

        return Ok(new { saved, errors });
    }
}

public class ScrapeRequest
{
    public List<string> Asins { get; set; } = new();
}
