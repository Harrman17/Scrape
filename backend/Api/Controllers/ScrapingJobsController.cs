using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AmazonScraper.Api.Models;
using AmazonScraper.Api.Services;

namespace AmazonScraper.Api.Controllers;

[ApiController]
[Route("api/scraping-jobs")]
[Authorize]
public class ScrapingJobsController : ControllerBase
{
    private readonly ScrapingJobsRepository _jobs;

    public ScrapingJobsController(ScrapingJobsRepository jobs)
    {
        _jobs = jobs;
    }

    [HttpGet]
    public async Task<ActionResult<List<ScrapingJob>>> GetJobs()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "User ID not found in token." });

        var jobs = await _jobs.GetByUserAsync(userId.Value);
        return Ok(jobs);
    }

    private long? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return long.TryParse(claim, out var id) ? id : null;
    }
}
