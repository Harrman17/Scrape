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
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { error = "A product URL is required." });
        }

        var python = "/Users/harman/Projects/Scrape/.venv/bin/Python";
        var script = "/Users/harman/Projects/Scrape/backend/py/amzProductScrape.py";
        var startInfo = new ProcessStartInfo
        {
            FileName = python,
            Arguments = $"\"{script}\" \"{request.Url}\"",
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
    public string Url { get; set; } = string.Empty;
}
