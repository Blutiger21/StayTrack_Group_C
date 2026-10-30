using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// Room within a property (e.g. single, sharing) with status vacant or occupied.
/// </summary>
public class Room
{
    [JsonPropertyName("room_id")]
    public int RoomId { get; set; }

    [JsonPropertyName("property_id")]
    public int PropertyId { get; set; }

    [JsonPropertyName("room_number")]
    public string RoomNumber { get; set; } = string.Empty;

    [JsonPropertyName("room_type")]
    public string RoomType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "vacant";

    // Relation joined via PostgREST embedding: property(...)
    [JsonPropertyName("property")]
    public Property? Property { get; set; }

    [JsonPropertyName("properties")]
    public Property? PropertyAlt
    {
        get => Property;
        set { if (Property == null) Property = value; }
    }
}
