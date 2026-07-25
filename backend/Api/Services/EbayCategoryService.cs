using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmazonScraper.Api.Services;

/// <summary>
/// Calls the eBay Taxonomy API to suggest the best-matching eBay category for a product.
/// Uses client-credentials OAuth with an in-memory token cache (tokens last ~2 hours).
/// </summary>
public class EbayCategoryService
{
    private readonly HttpClient _http;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly ILogger<EbayCategoryService> _logger;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    // UK eBay marketplace category tree ID
    private const int UkCategoryTreeId = 3;

    public EbayCategoryService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<EbayCategoryService> logger)
    {
        _http = httpClientFactory.CreateClient("ebay");
        _clientId = config["Ebay:ClientId"];
        _clientSecret = config["Ebay:ClientSecret"];
        _logger = logger;
    }

    /// <summary>True when ClientId and ClientSecret are present in config.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_clientSecret);

    /// <summary>
    /// Returns the top eBay category suggestion for the given product title.
    /// Returns (null, null) silently if eBay is not configured or the API call fails.
    /// </summary>
    public async Task<(string? CategoryId, string? CategoryName)> SuggestCategoryAsync(string title)
    {
        if (!IsConfigured) return (null, null);

        try
        {
            var token = await GetTokenAsync();
            if (token == null) return (null, null);

            var url = $"https://api.ebay.com/commerce/taxonomy/v1/category_tree/{UkCategoryTreeId}/get_category_suggestions"
                    + $"?q={Uri.EscapeDataString(title)}";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("eBay category suggestion returned {Status} for title: {Title}",
                    response.StatusCode, title);
                return (null, null);
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EbayCategorySuggestionsResponse>(json);
            var top = result?.CategorySuggestions?.FirstOrDefault();
            if (top?.Category == null) return (null, null);

            return (top.Category.CategoryId, top.Category.CategoryName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "eBay category suggestion threw for title: {Title}", title);
            return (null, null);
        }
    }

    // ── OAuth token (client credentials) ───────────────────────────────────────

    private async Task<string?> GetTokenAsync()
    {
        // Fast path: valid cached token
        if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiry)
            return _cachedToken;

        await _tokenLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiry)
                return _cachedToken;

            var credentials = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"]      = "https://api.ebay.com/oauth/api_scope",
            });

            var response = await _http.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("eBay OAuth token request failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<EbayTokenResponse>(json);
            if (tokenResponse?.AccessToken == null) return null;

            _cachedToken = tokenResponse.AccessToken;
            // Expire 5 minutes early to avoid edge-case failures
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300);

            _logger.LogInformation("eBay OAuth token refreshed, expires at {Expiry}", _tokenExpiry);
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    // ── Private response DTOs ──────────────────────────────────────────────────

    private sealed class EbayTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class EbayCategorySuggestionsResponse
    {
        [JsonPropertyName("categorySuggestions")]
        public List<EbayCategorySuggestion>? CategorySuggestions { get; set; }
    }

    private sealed class EbayCategorySuggestion
    {
        [JsonPropertyName("category")]
        public EbayCategoryInfo? Category { get; set; }
    }

    private sealed class EbayCategoryInfo
    {
        [JsonPropertyName("categoryId")]
        public string? CategoryId { get; set; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }
    }
}
