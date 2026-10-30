using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// Tenant lease record linking a student user profile to a room.
/// </summary>
public class Tenant
{
    [JsonPropertyName("tenant_id")]
    public int TenantId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("room_id")]
    public int RoomId { get; set; }

    [JsonPropertyName("student_number")]
    public string StudentNumber { get; set; } = string.Empty;

    [JsonPropertyName("payment_reference")]
    public string PaymentReference { get; set; } = string.Empty;

    [JsonPropertyName("rental_amount")]
    public decimal RentalAmount { get; set; }

    [JsonPropertyName("lease_start")]
    public DateTime LeaseStart { get; set; }

    [JsonPropertyName("lease_end")]
    public DateTime LeaseEnd { get; set; }

    [JsonPropertyName("date_registered")]
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    // Relations joined via PostgREST embedding
    [JsonPropertyName("profiles")]
    public Profile? Profile { get; set; }

    [JsonPropertyName("profile")]
    public Profile? ProfileAlt
    {
        get => Profile;
        set { if (Profile == null) Profile = value; }
    }

    [JsonPropertyName("room")]
    public Room? Room { get; set; }

    [JsonPropertyName("rooms")]
    public Room? RoomsAlt
    {
        get => Room;
        set { if (Room == null) Room = value; }
    }
}
