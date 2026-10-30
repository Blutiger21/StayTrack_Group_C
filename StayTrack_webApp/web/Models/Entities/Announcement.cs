using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// Broadcast announcement created by management.
/// </summary>
public class Announcement
{
    [JsonPropertyName("announcement_id")]
    public int AnnouncementId { get; set; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relation joined via PostgREST embedding
    [JsonPropertyName("profiles")]
    public Profile? Profile { get; set; }

    [JsonPropertyName("creator")]
    public Profile? Creator
    {
        get => Profile ?? _creator;
        set => _creator = value;
    }
    private Profile? _creator;
}
