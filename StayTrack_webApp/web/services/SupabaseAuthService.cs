using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using StayTrack.Models;
using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Implements Supabase authentication using standard HTTP REST endpoints.
/// Handles OTP request, OTP token verification, role resolution from profiles,
/// token refresh, and server-side HttpOnly cookie session management.
/// </summary>
public class SupabaseAuthService : ISupabaseAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SupabaseOptions> _optionsMonitor;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SupabaseAuthService> _logger;

    public SupabaseAuthService(
        HttpClient httpClient,
        IOptionsMonitor<SupabaseOptions> optionsMonitor,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SupabaseAuthService> logger)
    {
        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private SupabaseOptions Options => _optionsMonitor.CurrentValue;

    private string CleanBaseUrl => (Options.Url ?? string.Empty).Trim().TrimEnd('/');

    private string? ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(Options.Url) ||
            Options.Url.Contains("YOUR_PROJECT_REF", StringComparison.OrdinalIgnoreCase))
        {
            return "Supabase URL is not configured. Please open web/appsettings.Development.json and replace 'https://YOUR_PROJECT_REF.supabase.co' with your real Supabase Project URL.";
        }

        if (string.IsNullOrWhiteSpace(Options.AnonKey) ||
            Options.AnonKey.Contains("YOUR_SUPABASE_ANON_KEY", StringComparison.OrdinalIgnoreCase))
        {
            return "Supabase AnonKey is not configured. Please open web/appsettings.Development.json and replace 'YOUR_SUPABASE_ANON_KEY' with your real Supabase publishable (anon) key.";
        }

        if (!Uri.TryCreate(Options.Url, UriKind.Absolute, out _))
        {
            return $"The Supabase URL '{Options.Url}' in appsettings.Development.json is not a valid absolute URL (it should start with https://).";
        }

        return null;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendOtpAsync(string email)
    {
        var configError = ValidateConfiguration();
        if (configError != null)
        {
            return (false, configError);
        }

        try
        {
            var url = $"{CleanBaseUrl}/auth/v1/otp";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", Options.AnonKey);

            var payload = new
            {
                email = email.Trim().ToLowerInvariant(),
                create_user = false // Prevents arbitrary user registration
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Supabase OTP send failed. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);

            var errorResponse = ParseError(errorContent);
            return (false, errorResponse.GetFriendlyMessage());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network/DNS error dispatching OTP to {Email}", email);
            return (false, $"Unable to connect to Supabase at '{CleanBaseUrl}'. Please check your internet connection and verify the project URL in web/appsettings.Development.json.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error dispatching OTP to {Email}", email);
            return (false, $"Unable to communicate with the authentication server: {ex.Message}");
        }
    }

    public async Task<(bool Success, SupabaseTokenResponse? Data, string? ErrorMessage)> VerifyOtpAsync(string email, string token)
    {
        var configError = ValidateConfiguration();
        if (configError != null)
        {
            return (false, null, configError);
        }

        try
        {
            var url = $"{CleanBaseUrl}/auth/v1/verify";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", Options.AnonKey);

            var payload = new
            {
                type = "email",
                email = email.Trim().ToLowerInvariant(),
                token = token.Trim()
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenData = JsonSerializer.Deserialize<SupabaseTokenResponse>(responseContent);
                return (true, tokenData, null);
            }

            _logger.LogWarning("Supabase OTP verification failed: {ResponseContent}", responseContent);
            var errorResponse = ParseError(responseContent);
            return (false, null, errorResponse.GetFriendlyMessage());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying OTP for {Email}", email);
            return (false, null, "An error occurred while verifying your code. Please try again.");
        }
    }

    public async Task<Profile?> GetUserProfileAsync(string accessToken, Guid userId)
    {
        try
        {
            // Join profiles with role using PostgREST embedding
            var url = $"{CleanBaseUrl}/rest/v1/profiles?id=eq.{userId}&select=*,role(*)";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("apikey", Options.AnonKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve profile for user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var profiles = JsonSerializer.Deserialize<List<Profile>>(content);
            var profile = profiles?.FirstOrDefault();

            if (profile != null && profile.Role == null && profile.RoleId > 0)
            {
                // Fallback: Query role table directly if embedding name differed
                var roleUrl = $"{CleanBaseUrl}/rest/v1/role?role_id=eq.{profile.RoleId}&select=*";
                using var roleRequest = new HttpRequestMessage(HttpMethod.Get, roleUrl);
                roleRequest.Headers.Add("apikey", Options.AnonKey);
                roleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var roleResponse = await _httpClient.SendAsync(roleRequest);
                if (roleResponse.IsSuccessStatusCode)
                {
                    var roleContent = await roleResponse.Content.ReadAsStringAsync();
                    var roles = JsonSerializer.Deserialize<List<Role>>(roleContent);
                    profile.Role = roles?.FirstOrDefault();
                }
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception fetching profile for user {UserId}", userId);
            return null;
        }
    }

    public async Task<(bool Success, SupabaseTokenResponse? Data, string? ErrorMessage)> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var url = $"{CleanBaseUrl}/auth/v1/token?grant_type=refresh_token";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", Options.AnonKey);

            var payload = new { refresh_token = refreshToken };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenData = JsonSerializer.Deserialize<SupabaseTokenResponse>(content);
                return (true, tokenData, null);
            }

            return (false, null, ParseError(content).GetFriendlyMessage());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception refreshing token");
            return (false, null, "Could not refresh session token.");
        }
    }

    public async Task<string?> GetValidAccessTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        var accessToken = httpContext.Session.GetString("access_token")
            ?? await httpContext.GetTokenAsync("access_token");

        var refreshToken = httpContext.Session.GetString("refresh_token")
            ?? await httpContext.GetTokenAsync("refresh_token");

        var expiresAtStr = httpContext.Session.GetString("token_expires_at")
            ?? await httpContext.GetTokenAsync("expires_at");

        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }

        // If expires_at is present, verify expiration
        if (DateTimeOffset.TryParse(expiresAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt))
        {
            // If token expires in less than 60 seconds and we have a refresh token, refresh it
            if (expiresAt <= DateTimeOffset.UtcNow.AddSeconds(60) && !string.IsNullOrEmpty(refreshToken))
            {
                var (refreshed, newTokens, _) = await RefreshTokenAsync(refreshToken);
                if (refreshed && newTokens != null)
                {
                    var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(newTokens.ExpiresIn);

                    // Update session
                    httpContext.Session.SetString("access_token", newTokens.AccessToken);
                    httpContext.Session.SetString("refresh_token", newTokens.RefreshToken);
                    httpContext.Session.SetString("token_expires_at", newExpiresAt.ToString("o"));

                    return newTokens.AccessToken;
                }
            }
        }

        return accessToken;
    }

    public async Task SignInAsync(Profile profile, SupabaseTokenResponse tokenResponse)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is unavailable for signing in.");

        var roleName = profile.Role?.RoleName?.ToLowerInvariant() ?? string.Empty;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profile.Id.ToString()),
            new(ClaimTypes.Email, profile.Email),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(profile.FullName) ? profile.Email : profile.FullName),
            new(ClaimTypes.Role, roleName),
            new("FullName", string.IsNullOrWhiteSpace(profile.FullName) ? profile.Email : profile.FullName),
            new("RoleId", profile.RoleId.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = expiresAt
        };

        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = tokenResponse.RefreshToken },
            new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o") }
        });

        // Store tokens securely in server-side session as well
        httpContext.Session.SetString("access_token", tokenResponse.AccessToken);
        httpContext.Session.SetString("refresh_token", tokenResponse.RefreshToken);
        httpContext.Session.SetString("token_expires_at", expiresAt.ToString("o"));
        httpContext.Session.SetString("user_id", profile.Id.ToString());
        httpContext.Session.SetString("user_role", roleName);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    public async Task SignOutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            httpContext.Session.Clear();
        }
    }

    private static SupabaseErrorResponse ParseError(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SupabaseErrorResponse>(json) ?? new SupabaseErrorResponse();
        }
        catch
        {
            return new SupabaseErrorResponse { Message = json };
        }
    }
}
