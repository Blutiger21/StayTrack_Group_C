using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// Maintenance technician record linking technician_id to a user profile.
/// </summary>
public class Technician
{
    [JsonPropertyName("technician_id")]
    public int TechnicianId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    // Relations joined via PostgREST embedding
    [JsonPropertyName("profiles")]
    public Profile? Profile { get; set; }

    [JsonPropertyName("profile")]
    public Profile? ProfileAlt
    {
        get => Profile;
        set { if (Profile == null) Profile = value; }
    }
}
