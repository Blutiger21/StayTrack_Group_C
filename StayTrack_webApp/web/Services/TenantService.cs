using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StayTrack.Models;
using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service managing tenant leases, properties, and rooms via the Supabase REST API.
/// Every call carries the signed-in manager's JWT access token so PostgreSQL
/// Row Level Security policies are applied.
/// </summary>
public class TenantService : ITenantService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SupabaseOptions> _optionsMonitor;
    private readonly ISupabaseAuthService _authService;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        HttpClient httpClient,
        IOptionsMonitor<SupabaseOptions> optionsMonitor,
        ISupabaseAuthService authService,
        ILogger<TenantService> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _authService = authService;
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

    public async Task<List<Tenant>> GetTenantsAsync(string? searchQuery = null)
    {
        try
        {
            // Query tenants with joined profiles and room (including property)
            // PostgREST embedding: profiles(id,full_name,email,phone), room(*,property(*))
            var query = "tenant?select=*,profiles:user_id(id,full_name,email,phone),room:room_id(*,property:property_id(*))&order=date_registered.desc";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                // Fallback to alternative embedding format if foreign key name differed
                var fallbackQuery = "tenant?select=*,profiles(*),room(*,property(*))&order=date_registered.desc";
                using var fallbackRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallbackQuery);
                response = await _httpClient.SendAsync(fallbackRequest);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to query tenants. Status: {StatusCode}", response.StatusCode);
                return new List<Tenant>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var tenants = JsonSerializer.Deserialize<List<Tenant>>(content) ?? new List<Tenant>();

            // Apply in-memory search filter if provided
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var queryLower = searchQuery.Trim().ToLowerInvariant();
                tenants = tenants.Where(t =>
                    (t.Profile?.FullName?.ToLowerInvariant().Contains(queryLower) ?? false) ||
                    (t.Profile?.Email?.ToLowerInvariant().Contains(queryLower) ?? false) ||
                    t.StudentNumber.ToLowerInvariant().Contains(queryLower) ||
                    t.PaymentReference.ToLowerInvariant().Contains(queryLower) ||
                    (t.Room?.RoomNumber.ToLowerInvariant().Contains(queryLower) ?? false) ||
                    (t.Room?.Property?.Name.ToLowerInvariant().Contains(queryLower) ?? false)
                ).ToList();
            }

            return tenants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenants from Supabase");
            return new List<Tenant>();
        }
    }

    public async Task<Tenant?> GetTenantByIdAsync(int tenantId)
    {
        try
        {
            var query = $"tenant?tenant_id=eq.{tenantId}&select=*,profiles:user_id(*),room:room_id(*,property:property_id(*))";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var fallbackQuery = $"tenant?tenant_id=eq.{tenantId}&select=*,profiles(*),room(*,property(*))";
                using var fallbackRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallbackQuery);
                response = await _httpClient.SendAsync(fallbackRequest);
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Tenant>>(content);
            return list?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<List<Profile>> GetUnassignedStudentProfilesAsync()
    {
        try
        {
            // 1. Find role_id for student role
            var roleQuery = "role?role_name=eq.student&select=role_id";
            using var roleRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, roleQuery);
            var roleResponse = await _httpClient.SendAsync(roleRequest);

            int studentRoleId = 1; // Default
            if (roleResponse.IsSuccessStatusCode)
            {
                var roleContent = await roleResponse.Content.ReadAsStringAsync();
                var roles = JsonSerializer.Deserialize<List<Role>>(roleContent);
                if (roles?.Any() == true)
                {
                    studentRoleId = roles.First().RoleId;
                }
            }

            // 2. Fetch all active student profiles
            var profileQuery = $"profiles?role_id=eq.{studentRoleId}&is_active=eq.true&select=*&order=full_name.asc";
            using var profileRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, profileQuery);
            var profileResponse = await _httpClient.SendAsync(profileRequest);

            if (!profileResponse.IsSuccessStatusCode)
            {
                return new List<Profile>();
            }

            var profileContent = await profileResponse.Content.ReadAsStringAsync();
            var allStudents = JsonSerializer.Deserialize<List<Profile>>(profileContent) ?? new List<Profile>();

            // 3. Fetch existing tenant user IDs (active)
            var tenantQuery = "tenant?status=eq.active&select=user_id";
            using var tenantRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, tenantQuery);
            var tenantResponse = await _httpClient.SendAsync(tenantRequest);

            var assignedUserIds = new HashSet<Guid>();
            if (tenantResponse.IsSuccessStatusCode)
            {
                var tenantContent = await tenantResponse.Content.ReadAsStringAsync();
                var activeTenants = JsonSerializer.Deserialize<List<Tenant>>(tenantContent) ?? new List<Tenant>();
                foreach (var t in activeTenants)
                {
                    assignedUserIds.Add(t.UserId);
                }
            }

            // Return students without an active lease
            return allStudents.Where(s => !assignedUserIds.Contains(s.Id)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unassigned student profiles");
            return new List<Profile>();
        }
    }

    public async Task<List<Property>> GetPropertiesAsync()
    {
        try
        {
            var query = "property?select=*&order=name.asc";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new List<Property>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Property>>(content) ?? new List<Property>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching properties");
            return new List<Property>();
        }
    }

    public async Task<List<Room>> GetRoomsAsync()
    {
        try
        {
            var query = "room?select=*,property:property_id(*)&order=room_number.asc";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var fallback = "room?select=*,property(*)&order=room_number.asc";
                using var fallbackRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, fallback);
                response = await _httpClient.SendAsync(fallbackRequest);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new List<Room>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Room>>(content) ?? new List<Room>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rooms");
            return new List<Room>();
        }
    }

    public async Task<string> GeneratePaymentReferenceAsync()
    {
        try
        {
            // Find existing references to suggest a unique next sequence
            var query = "tenant?select=payment_reference&order=tenant_id.desc&limit=20";
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, query);
            var response = await _httpClient.SendAsync(request);

            var existingRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tenants = JsonSerializer.Deserialize<List<Tenant>>(content);
                if (tenants != null)
                {
                    foreach (var t in tenants)
                    {
                        if (!string.IsNullOrWhiteSpace(t.PaymentReference))
                        {
                            existingRefs.Add(t.PaymentReference.Trim());
                        }
                    }
                }
            }

            // Generate "STAY-XXXX"
            var random = new Random();
            for (int i = 0; i < 50; i++)
            {
                var candidate = $"STAY-{random.Next(1001, 9999)}";
                if (!existingRefs.Contains(candidate))
                {
                    return candidate;
                }
            }

            return $"STAY-{DateTime.UtcNow:MMddHHmm}";
        }
        catch
        {
            return $"STAY-{Random.Shared.Next(1000, 9999)}";
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateTenantAsync(Tenant tenant)
    {
        try
        {
            // 1. Verify Payment Reference is unique
            var checkRefQuery = $"tenant?payment_reference=eq.{Uri.EscapeDataString(tenant.PaymentReference)}&select=tenant_id";
            using var checkRefReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, checkRefQuery);
            var checkRefRes = await _httpClient.SendAsync(checkRefReq);

            if (checkRefRes.IsSuccessStatusCode)
            {
                var content = await checkRefRes.Content.ReadAsStringAsync();
                var existing = JsonSerializer.Deserialize<List<Tenant>>(content);
                if (existing?.Any() == true)
                {
                    return (false, $"The payment reference '{tenant.PaymentReference}' is already in use. Please enter a unique reference.");
                }
            }

            // 2. Insert new tenant
            var insertPayload = new
            {
                user_id = tenant.UserId,
                room_id = tenant.RoomId,
                student_number = tenant.StudentNumber.Trim(),
                payment_reference = tenant.PaymentReference.Trim(),
                rental_amount = tenant.RentalAmount,
                lease_start = tenant.LeaseStart.ToString("yyyy-MM-dd"),
                lease_end = tenant.LeaseEnd.ToString("yyyy-MM-dd"),
                status = string.IsNullOrWhiteSpace(tenant.Status) ? "active" : tenant.Status
            };

            using var insertReq = await CreateAuthorizedRequestAsync(HttpMethod.Post, "tenant");
            insertReq.Headers.Add("Prefer", "return=representation");
            insertReq.Content = new StringContent(JsonSerializer.Serialize(insertPayload), Encoding.UTF8, "application/json");

            var insertRes = await _httpClient.SendAsync(insertReq);
            if (!insertRes.IsSuccessStatusCode)
            {
                var err = await insertRes.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to insert tenant. Status: {StatusCode}, Error: {Error}", insertRes.StatusCode, err);
                return (false, "Could not save tenant. Supabase error: " + ParsePostgrestError(err));
            }

            // 3. Mark the assigned room as 'occupied'
            try
            {
                var updateRoomPayload = new { status = "occupied" };
                using var roomReq = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"room?room_id=eq.{tenant.RoomId}");
                roomReq.Content = new StringContent(JsonSerializer.Serialize(updateRoomPayload), Encoding.UTF8, "application/json");
                await _httpClient.SendAsync(roomReq);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update room {RoomId} status to occupied", tenant.RoomId);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateTenantAsync(Tenant tenant)
    {
        try
        {
            // 1. Verify Payment Reference is unique (excluding this tenant)
            var checkRefQuery = $"tenant?payment_reference=eq.{Uri.EscapeDataString(tenant.PaymentReference)}&tenant_id=neq.{tenant.TenantId}&select=tenant_id";
            using var checkRefReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, checkRefQuery);
            var checkRefRes = await _httpClient.SendAsync(checkRefReq);

            if (checkRefRes.IsSuccessStatusCode)
            {
                var content = await checkRefRes.Content.ReadAsStringAsync();
                var existing = JsonSerializer.Deserialize<List<Tenant>>(content);
                if (existing?.Any() == true)
                {
                    return (false, $"The payment reference '{tenant.PaymentReference}' is already in use by another tenant.");
                }
            }

            // 2. Fetch existing record to check if room changed
            var previous = await GetTenantByIdAsync(tenant.TenantId);

            // 3. Update tenant
            var updatePayload = new
            {
                room_id = tenant.RoomId,
                student_number = tenant.StudentNumber.Trim(),
                payment_reference = tenant.PaymentReference.Trim(),
                rental_amount = tenant.RentalAmount,
                lease_start = tenant.LeaseStart.ToString("yyyy-MM-dd"),
                lease_end = tenant.LeaseEnd.ToString("yyyy-MM-dd"),
                status = tenant.Status
            };

            using var updateReq = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"tenant?tenant_id=eq.{tenant.TenantId}");
            updateReq.Content = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");

            var updateRes = await _httpClient.SendAsync(updateReq);
            if (!updateRes.IsSuccessStatusCode)
            {
                var err = await updateRes.Content.ReadAsStringAsync();
                return (false, "Could not update tenant: " + ParsePostgrestError(err));
            }

            // 4. If room changed, update room statuses
            if (previous != null && previous.RoomId != tenant.RoomId)
            {
                // New room is occupied
                var newRoomPayload = new { status = "occupied" };
                using var newRoomReq = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"room?room_id=eq.{tenant.RoomId}");
                newRoomReq.Content = new StringContent(JsonSerializer.Serialize(newRoomPayload), Encoding.UTF8, "application/json");
                await _httpClient.SendAsync(newRoomReq);

                // Previous room: check if other active tenants exist
                await RefreshRoomOccupancyStatusAsync(previous.RoomId);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenant.TenantId);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ArchiveTenantAsync(int tenantId)
    {
        try
        {
            var tenant = await GetTenantByIdAsync(tenantId);
            if (tenant == null)
            {
                return (false, "Tenant not found.");
            }

            // Set tenant status to inactive
            var payload = new { status = "inactive" };
            using var req = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"tenant?tenant_id=eq.{tenantId}");
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res = await _httpClient.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, "Failed to archive tenant: " + ParsePostgrestError(err));
            }

            // Refresh room occupancy status
            await RefreshRoomOccupancyStatusAsync(tenant.RoomId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving tenant {TenantId}", tenantId);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateRoomAsync(Room room)
    {
        try
        {
            // Check if room number already exists in that property
            var checkQuery = $"room?property_id=eq.{room.PropertyId}&room_number=eq.{Uri.EscapeDataString(room.RoomNumber)}&select=room_id";
            using var checkReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, checkQuery);
            var checkRes = await _httpClient.SendAsync(checkReq);

            if (checkRes.IsSuccessStatusCode)
            {
                var content = await checkRes.Content.ReadAsStringAsync();
                var existing = JsonSerializer.Deserialize<List<Room>>(content);
                if (existing?.Any() == true)
                {
                    return (false, $"Room '{room.RoomNumber}' already exists in this property.");
                }
            }

            var payload = new
            {
                property_id = room.PropertyId,
                room_number = room.RoomNumber.Trim(),
                room_type = room.RoomType.Trim(),
                status = string.IsNullOrWhiteSpace(room.Status) ? "vacant" : room.Status.Trim().ToLowerInvariant()
            };

            using var req = await CreateAuthorizedRequestAsync(HttpMethod.Post, "room");
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res = await _httpClient.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, "Could not add room: " + ParsePostgrestError(err));
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CreatePropertyAsync(Property property)
    {
        try
        {
            var payload = new
            {
                name = property.Name.Trim(),
                address = property.Address.Trim()
            };

            using var req = await CreateAuthorizedRequestAsync(HttpMethod.Post, "property");
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res = await _httpClient.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, "Could not add property: " + ParsePostgrestError(err));
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property");
            return (false, ex.Message);
        }
    }

    private async Task RefreshRoomOccupancyStatusAsync(int roomId)
    {
        try
        {
            // Check if any other active tenant occupies this room
            var checkQuery = $"tenant?room_id=eq.{roomId}&status=eq.active&select=tenant_id";
            using var checkReq = await CreateAuthorizedRequestAsync(HttpMethod.Get, checkQuery);
            var checkRes = await _httpClient.SendAsync(checkReq);

            var isOccupied = false;
            if (checkRes.IsSuccessStatusCode)
            {
                var content = await checkRes.Content.ReadAsStringAsync();
                var activeTenants = JsonSerializer.Deserialize<List<Tenant>>(content);
                isOccupied = activeTenants?.Any() == true;
            }

            var newStatus = isOccupied ? "occupied" : "vacant";
            var payload = new { status = newStatus };
            using var patchReq = await CreateAuthorizedRequestAsync(HttpMethod.Patch, $"room?room_id=eq.{roomId}");
            patchReq.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            await _httpClient.SendAsync(patchReq);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh room {RoomId} occupancy status", roomId);
        }
    }

    private static string ParsePostgrestError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                return msg.GetString() ?? json;
            }
        }
        catch { }
        return json;
    }
}
