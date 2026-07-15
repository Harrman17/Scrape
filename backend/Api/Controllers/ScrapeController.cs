using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AmazonScraper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScrapeController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<object>> Post([FromBody] ScrapeRequest request)
    {
        if (request.Asins is null || request.Asins.Count == 0)
        {
            return BadRequest(new { error = "At least one ASIN is required." });
        }

        var python = "C:/Development/Scrape/backend/py/.venv/Scripts/python.exe";
        var script = "C:/Development/Scrape/backend/py/amzProductScrape.py";
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
        {
            return StatusCode(500, new { error = "Failed to start Python scraper." });
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            return StatusCode(500, new { error = stderr.Trim(), stdout });
        }

        return Ok(System.Text.Json.JsonSerializer.Deserialize<object>(stdout));
    }
}

public class ScrapeRequest
{
    public List<string> Asins { get; set; } = new();
}
