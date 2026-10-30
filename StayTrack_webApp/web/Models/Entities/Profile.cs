using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// User profile corresponding to a Supabase auth.users record.
/// </summary>
public class Profile
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("role_id")]
    public int RoleId { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    // Optional relation joined via PostgREST embedding: role(role_id, role_name)
    [JsonPropertyName("role")]
    public Role? Role { get; set; }
}
