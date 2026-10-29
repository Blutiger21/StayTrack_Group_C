using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StayTrack.Models;
using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service managing broadcast announcements via Supabase REST API.
/// Carries the manager's JWT access token for Row Level Security.
/// Supports resilient embedding joins, plain fallback queries, and profile enrichment.
/// </summary>
public class AnnouncementService : IAnnouncementService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SupabaseOptions> _optionsMonitor;
    private readonly ISupabaseAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AnnouncementService> _logger;

    public AnnouncementService(
        HttpClient httpClient,
        IOptionsMonitor<SupabaseOptions> optionsMonitor,
        ISupabaseAuthService authService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AnnouncementService> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _authService = authService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private SupabaseOptions Options => _optionsMonitor.CurrentValue;

    private string CleanBaseUrl => (Options.Url ?? string.Empty).Trim().TrimEnd('/');

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string pathAndQuery)
    {
        var token = await _authService.GetValidAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("Your session has expired. Please sign in again.");
        }

        var url = $"{CleanBaseUrl}/rest/v1/{pathAndQuery.TrimStart('/')}";
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", Options.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<List<Announcement>> GetAnnouncementsAsync()
    {
        try
        {
            // Primary query: embed profiles via foreign key created_by (matches Flutter app)
            var query = "announcement?select=*,profiles:created_by(*)&order=created_at.desc";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Primary announcements query failed (Status: {StatusCode}). Trying fallback...", response.StatusCode);
                // Fallback: Query table directly without relation join to guarantee results
                var fallback = "announcement?select=*&order=created_at.desc";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to query announcements. Status: {StatusCode}, Error: {Error}", response.StatusCode, err);
                return new List<Announcement>();
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Announcements query received ({StatusCode}): {Content}", response.StatusCode, content);
            var list = JsonSerializer.Deserialize<List<Announcement>>(content) ?? new List<Announcement>();

            // If creator profile was not embedded, enrich profiles directly
            if (list.Any(a => a.Creator == null && a.CreatedBy != Guid.Empty))
            {
                await EnrichProfilesAsync(list);
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching announcements");
            return new List<Announcement>();
        }
    }

    public async Task<Announcement?> GetLatestAnnouncementAsync()
    {
        try
        {
            // Primary query: embed profiles via foreign key created_by
            var query = "announcement?select=*,profiles:created_by(*)&order=created_at.desc&limit=1";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var fallback = "announcement?select=*&order=created_at.desc&limit=1";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Announcement>>(content);
            var item = list?.FirstOrDefault();

            if (item != null && item.Creator == null && item.CreatedBy != Guid.Empty)
            {
                await EnrichProfilesAsync(new List<Announcement> { item });
            }

            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest announcement");
            return null;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAnnouncementAsync(string title, string message, Guid createdByUserId)
    {
        try
        {
            // Fallback resolution for current user ID if not provided
            if (createdByUserId == Guid.Empty)
            {
                var idStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
                if (Guid.TryParse(idStr, out var parsedId))
                {
                    createdByUserId = parsedId;
                }
                else
                {
                    var token = await _authService.GetValidAccessTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        createdByUserId = ExtractUserIdFromToken(token);
                    }
                }
            }

            _logger.LogInformation("Sending POST announcement to Supabase: Title='{Title}', CreatedBy='{CreatedBy}'", title, createdByUserId);

            var payload = new Dictionary<string, object>
            {
                ["title"] = title.Trim(),
                ["message"] = message.Trim()
            };

            if (createdByUserId != Guid.Empty)
            {
                payload["created_by"] = createdByUserId;
            }

            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, "announcement");
            request.Headers.Add("Prefer", "return=representation");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var respBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create announcement in Supabase ({Status}): {Error}", response.StatusCode, respBody);
                return (false, "Could not post announcement: " + respBody);
            }

            _logger.LogInformation("Announcement posted successfully to Supabase ({Status}): {Body}", response.StatusCode, respBody);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating announcement");
            return (false, ex.Message);
        }
    }

    private static Guid ExtractUserIdFromToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return Guid.Empty;
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=').Replace('-', '+').Replace('_', '/');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("sub", out var subProp) && Guid.TryParse(subProp.GetString(), out var subGuid))
            {
                return subGuid;
            }
        }
        catch {}
        return Guid.Empty;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAnnouncementAsync(int announcementId)
    {
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"announcement?announcement_id=eq.{announcementId}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, "Could not delete announcement: " + err);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting announcement {Id}", announcementId);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Helper to enrich announcement records with creator full names if relation join was bypassed.
    /// </summary>
    private async Task EnrichProfilesAsync(List<Announcement> announcements)
    {
        try
        {
            var userIds = announcements
                .Where(a => a.Creator == null && a.CreatedBy != Guid.Empty)
                .Select(a => a.CreatedBy)
                .Distinct()
                .ToList();

            if (!userIds.Any()) return;

            var inFilter = string.Join(",", userIds);
            var query = $"profiles?id=in.({inFilter})&select=id,full_name,email";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var profiles = JsonSerializer.Deserialize<List<Profile>>(content) ?? new List<Profile>();
                var profileMap = profiles.ToDictionary(p => p.Id, p => p);

                foreach (var a in announcements)
                {
                    if (a.Creator == null && profileMap.TryGetValue(a.CreatedBy, out var profile))
                    {
                        a.Creator = profile;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enrich announcement creator profiles");
        }
    }
}
