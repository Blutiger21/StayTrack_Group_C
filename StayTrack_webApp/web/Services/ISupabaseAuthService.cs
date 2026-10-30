using StayTrack.Models;
using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service interface for Supabase GoTrue authentication and user profile retrieval.
/// Manages OTP dispatch, OTP verification, token refresh, and server-side cookie authentication.
/// </summary>
public interface ISupabaseAuthService
{
    /// <summary>
    /// Sends a one-time password (OTP) to the specified staff email.
    /// Uses create_user: false so unregistered users cannot create new accounts.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> SendOtpAsync(string email);

    /// <summary>
    /// Verifies the OTP token against Supabase Auth.
    /// </summary>
    Task<(bool Success, SupabaseTokenResponse? Data, string? ErrorMessage)> VerifyOtpAsync(string email, string token);

    /// <summary>
    /// Fetches the profile and role from the PostgreSQL profiles table using the user's JWT access token.
    /// </summary>
    Task<Profile?> GetUserProfileAsync(string accessToken, Guid userId);

    /// <summary>
    /// Refreshes an expired JWT access token using the refresh token stored in session/auth cookies.
    /// </summary>
    Task<(bool Success, SupabaseTokenResponse? Data, string? ErrorMessage)> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Resolves the current user's valid JWT access token, automatically refreshing if expired.
    /// </summary>
    Task<string?> GetValidAccessTokenAsync();

    /// <summary>
    /// Establishes the ASP.NET Core HttpOnly cookie authentication ticket with user claims and tokens.
    /// </summary>
    Task SignInAsync(Profile profile, SupabaseTokenResponse tokenResponse);

    /// <summary>
    /// Signs the user out, clearing cookie authentication and session state.
    /// </summary>
    Task SignOutAsync();
}
