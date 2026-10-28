using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StayTrack.Models;
using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service managing maintenance requests, technician assignments, status workflows,
/// and private storage photo signing. Honors strict PostgreSQL RLS policies.
/// </summary>
public class MaintenanceService : IMaintenanceService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SupabaseOptions> _optionsMonitor;
    private readonly ISupabaseAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        HttpClient httpClient,
        IOptionsMonitor<SupabaseOptions> optionsMonitor,
        ISupabaseAuthService authService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MaintenanceService> logger)
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

    // --- Management Operations ---

    public async Task<List<MaintenanceRequest>> GetAllRequestsAsync(string? statusFilter = null, string? search = null)
    {
        try
        {
            var filter = string.Empty;
            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                filter = $"&status=eq.{statusFilter.Trim().ToLowerInvariant()}";
            }

            var query = $"maintenance_request?select=*,tenant:tenant_id(*,profiles:user_id(*),room:room_id(*,property:property_id(*))),technician:technician_id(*,profiles:user_id(*)){filter}&order=date_submitted.desc";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var fallback = $"maintenance_request?select=*,tenant(*,profiles(*),room(*,property(*))),technician(*,profiles(*)){filter}&order=date_submitted.desc";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to query maintenance requests. Status: {StatusCode}", response.StatusCode);
                return new List<MaintenanceRequest>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var requests = JsonSerializer.Deserialize<List<MaintenanceRequest>>(content) ?? new List<MaintenanceRequest>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLowerInvariant();
                requests = requests.Where(r =>
                    r.Title.ToLowerInvariant().Contains(q) ||
                    r.Description.ToLowerInvariant().Contains(q) ||
                    (r.Tenant?.Profile?.FullName?.ToLowerInvariant().Contains(q) ?? false) ||
                    (r.Tenant?.Room?.RoomNumber?.ToLowerInvariant().Contains(q) ?? false) ||
                    (r.Technician?.Profile?.FullName?.ToLowerInvariant().Contains(q) ?? false)
                ).ToList();
            }

            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance requests");
            return new List<MaintenanceRequest>();
        }
    }

    public async Task<MaintenanceRequest?> GetRequestByIdAsync(int requestId)
    {
        try
        {
            var query = $"maintenance_request?request_id=eq.{requestId}&select=*,tenant:tenant_id(*,profiles:user_id(*),room:room_id(*,property:property_id(*))),technician:technician_id(*,profiles:user_id(*))";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var fallback = $"maintenance_request?request_id=eq.{requestId}&select=*,tenant(*,profiles(*),room(*,property(*))),technician(*,profiles(*))";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<MaintenanceRequest>>(content);
            return list?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance request {RequestId}", requestId);
            return null;
        }
    }

    public async Task<List<Technician>> GetTechniciansAsync()
    {
        try
        {
            var query = "technician?select=*,profiles:user_id(*)&order=technician_id.asc";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var fallback = "technician?select=*,profiles(*)&order=technician_id.asc";
                using var fallbackReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new List<Technician>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Technician>>(content) ?? new List<Technician>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching technicians");
            return new List<Technician>();
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> AssignTechnicianAsync(int requestId, int technicianId)
    {
        try
        {
            var payload = new
            {
                technician_id = technicianId,
                status = "assigned"
            };

            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"maintenance_request?request_id=eq.{requestId}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, "Could not assign technician: " + err);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning technician to request {RequestId}", requestId);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateStatusAsync(int requestId, string status, DateTime? dateCompleted = null)
    {
        try
        {
            var cleanStatus = status.Trim().ToLowerInvariant();
            object payload = cleanStatus switch
            {
                "completed" => new
                {
                    status = "completed",
                    date_completed = (dateCompleted ?? DateTime.UtcNow).ToString("o")
                },
                "pending" => new
                {
                    status = "pending",
                    date_completed = (string?)null
                },
                _ => new { status = cleanStatus }
            };

            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"maintenance_request?request_id=eq.{requestId}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, "Could not update status: " + err);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for request {RequestId}", requestId);
            return (false, ex.Message);
        }
    }

    public async Task<(int Total, int Pending, int Assigned, int Completed)> GetRequestCountsAsync()
    {
        try
        {
            var query = "maintenance_request?select=status";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return (0, 0, 0, 0);
            }

            var content = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<RequestStatusItem>>(content) ?? new List<RequestStatusItem>();

            var total = items.Count;
            var pending = items.Count(i => i.Status.Equals("pending", StringComparison.OrdinalIgnoreCase));
            var assigned = items.Count(i => i.Status.Equals("assigned", StringComparison.OrdinalIgnoreCase));
            var completed = items.Count(i => i.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));

            return (total, pending, assigned, completed);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    // --- Technician Operations (Strictly No Tenant Queries for RLS Safety) ---

    public async Task<int?> GetCurrentTechnicianIdAsync()
    {
        try
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return null;

            var query = $"technician?user_id=eq.{userId}&select=technician_id";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<Technician>>(content);
                return list?.FirstOrDefault()?.TechnicianId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<MaintenanceRequest>> GetAssignedRequestsForCurrentTechnicianAsync()
    {
        try
        {
            var techId = await GetCurrentTechnicianIdAsync();
            var filter = techId.HasValue ? $"technician_id=eq.{techId.Value}&" : string.Empty;

            // Notice: We select ONLY maintenance_request fields, NOT tenant, to respect RLS!
            var query = $"maintenance_request?{filter}select=request_id,title,description,photo_url,status,date_submitted,date_completed&order=date_submitted.desc";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to query technician requests. Status: {StatusCode}", response.StatusCode);
                return new List<MaintenanceRequest>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MaintenanceRequest>>(content) ?? new List<MaintenanceRequest>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching assigned requests for technician");
            return new List<MaintenanceRequest>();
        }
    }

    public async Task<MaintenanceRequest?> GetTechnicianRequestByIdAsync(int requestId)
    {
        try
        {
            var techId = await GetCurrentTechnicianIdAsync();
            var filter = techId.HasValue ? $"&technician_id=eq.{techId.Value}" : string.Empty;

            var query = $"maintenance_request?request_id=eq.{requestId}{filter}&select=request_id,title,description,photo_url,status,date_submitted,date_completed";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<MaintenanceRequest>>(content);
            return list?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching technician request {RequestId}", requestId);
            return null;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> MarkCompletedByTechnicianAsync(int requestId)
    {
        try
        {
            var payload = new
            {
                status = "completed",
                date_completed = DateTime.UtcNow.ToString("o")
            };

            // RLS ensures the technician can only update requests assigned to them
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"maintenance_request?request_id=eq.{requestId}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, "Could not mark as completed: " + err);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing request {RequestId}", requestId);
            return (false, ex.Message);
        }
    }

    // --- Shared Operations ---

    public async Task<string?> GetPhotoSignedUrlAsync(string? photoUrlPath)
    {
        if (string.IsNullOrWhiteSpace(photoUrlPath)) return null;

        if (photoUrlPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            photoUrlPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return photoUrlPath;
        }

        try
        {
            var token = await _authService.GetValidAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            var cleanPath = photoUrlPath.Trim().TrimStart('/');
            string bucket = "maintenance-photos";

            if (cleanPath.StartsWith("maintenance-photos/", StringComparison.OrdinalIgnoreCase))
            {
                bucket = "maintenance-photos";
                cleanPath = cleanPath.Substring("maintenance-photos/".Length);
            }
            else if (cleanPath.StartsWith("maintenance/", StringComparison.OrdinalIgnoreCase))
            {
                bucket = "maintenance";
                cleanPath = cleanPath.Substring("maintenance/".Length);
            }
            else if (cleanPath.Contains('/'))
            {
                var slashIdx = cleanPath.IndexOf('/');
                var possibleBucket = cleanPath.Substring(0, slashIdx);
                var remainder = cleanPath.Substring(slashIdx + 1);
                if (!string.IsNullOrWhiteSpace(possibleBucket) && !string.IsNullOrWhiteSpace(remainder))
                {
                    bucket = possibleBucket;
                    cleanPath = remainder;
                }
            }

            // Supabase Storage Sign Endpoint:
            // POST /storage/v1/object/sign/{bucket}/{path}
            var url = $"{CleanBaseUrl}/storage/v1/object/sign/{bucket}/{cleanPath}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", Options.AnonKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new { expiresIn = 3600 };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to generate signed URL for maintenance photo {Path}. Status: {StatusCode}, Error: {Error}", cleanPath, response.StatusCode, err);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("signedURL", out var signedElem))
            {
                var rel = signedElem.GetString();
                if (string.IsNullOrEmpty(rel)) return null;

                if (rel.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    rel.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return rel;
                }

                // Supabase Storage returns "/object/sign/...", which must be prefixed with "/storage/v1"
                var finalPath = rel.StartsWith("/") ? rel : "/" + rel;
                if (!finalPath.StartsWith("/storage/v1/", StringComparison.OrdinalIgnoreCase))
                {
                    finalPath = "/storage/v1" + finalPath;
                }

                return $"{CleanBaseUrl}{finalPath}";
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception generating signed URL for maintenance photo {Path}", photoUrlPath);
            return null;
        }
    }

    private class RequestStatusItem
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
