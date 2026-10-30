using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// Represents a user role in the system: student, management, or technician.
/// </summary>
public class Role
{
    [JsonPropertyName("role_id")]
    public int RoleId { get; set; }

    [JsonPropertyName("role_name")]
    public string RoleName { get; set; } = string.Empty;
}
