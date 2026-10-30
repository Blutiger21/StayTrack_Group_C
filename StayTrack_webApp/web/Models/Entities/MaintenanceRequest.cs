using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// Maintenance issue submitted by a tenant, assigned by management, and completed by a technician.
/// </summary>
public class MaintenanceRequest
{
    [JsonPropertyName("request_id")]
    public int RequestId { get; set; }

    [JsonPropertyName("tenant_id")]
    public int TenantId { get; set; }

    [JsonPropertyName("technician_id")]
    public int? TechnicianId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    [JsonPropertyName("date_submitted")]
    public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("date_completed")]
    public DateTime? DateCompleted { get; set; }

    // Relations joined via PostgREST embedding
    [JsonPropertyName("tenant")]
    public Tenant? Tenant { get; set; }

    [JsonPropertyName("tenants")]
    public Tenant? TenantAlt
    {
        get => Tenant;
        set { if (Tenant == null) Tenant = value; }
    }

    [JsonPropertyName("technician")]
    public Technician? Technician { get; set; }

    [JsonPropertyName("technicians")]
    public Technician? TechnicianAlt
    {
        get => Technician;
        set { if (Technician == null) Technician = value; }
    }
}
