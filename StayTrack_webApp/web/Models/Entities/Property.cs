using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// Student accommodation building or property.
/// </summary>
public class Property
{
    [JsonPropertyName("property_id")]
    public int PropertyId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;
}
