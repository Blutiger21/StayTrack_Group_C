using System.Text.Json.Serialization;

namespace StayTrack.Models;

/// <summary>
/// Supabase GoTrue authentication response containing JWT tokens and user metadata.
/// </summary>
public class SupabaseTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public SupabaseAuthUser? User { get; set; }
}

public class SupabaseAuthUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class SupabaseErrorResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("code")]
    public object? Code { get; set; }

    public string GetFriendlyMessage()
    {
        var raw = Message ?? Msg ?? ErrorDescription ?? string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return "An unexpected error occurred while communicating with the authentication service.";
        }

        if (raw.Contains("Signups not allowed", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("User not found", StringComparison.OrdinalIgnoreCase))
        {
            return "No registered staff account was found with this email address. Accounts must be registered by an administrator.";
        }

        if (raw.Contains("Token has expired or is invalid", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("otp_expired", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("invalid token", StringComparison.OrdinalIgnoreCase))
        {
            return "The verification code entered is invalid or has expired. Please request a new code.";
        }

        if (raw.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("over_email_send_rate_limit", StringComparison.OrdinalIgnoreCase))
        {
            return "Too many requests sent. Please wait a few moments before requesting another code.";
        }

        return raw;
    }
}
