namespace StayTrack.Models;

/// <summary>
/// Configuration settings for connecting to the Supabase backend.
/// Stored in appsettings.Development.json (gitignored) for local development.
/// </summary>
public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;

    public string AnonKey { get; set; } = string.Empty;
}
