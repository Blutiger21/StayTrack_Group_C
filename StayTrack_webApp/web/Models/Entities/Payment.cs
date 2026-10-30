using System.Text.Json.Serialization;

namespace StayTrack.Models.Entities;

/// <summary>
/// Accommodation rent payment record submitted by student or recorded by management.
/// </summary>
public class Payment
{
    [JsonPropertyName("payment_id")]
    public int PaymentId { get; set; }

    [JsonPropertyName("tenant_id")]
    public int TenantId { get; set; }

    [JsonPropertyName("verified_by")]
    public Guid? VerifiedBy { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("payment_date")]
    public DateTime PaymentDate { get; set; }

    [JsonPropertyName("period_month")]
    public DateTime PeriodMonth { get; set; }

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("proof_url")]
    public string? ProofUrl { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    [JsonPropertyName("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    // Relations joined via PostgREST embedding
    [JsonPropertyName("tenant")]
    public Tenant? Tenant { get; set; }

    [JsonPropertyName("tenants")]
    public Tenant? TenantAlt
    {
        get => Tenant;
        set { if (Tenant == null) Tenant = value; }
    }

    [JsonPropertyName("verified_by_profile")]
    public Profile? VerifiedByProfile { get; set; }

    [JsonPropertyName("profiles")]
    public Profile? VerifiedByProfileAlt
    {
        get => VerifiedByProfile;
        set { if (VerifiedByProfile == null) VerifiedByProfile = value; }
    }
}
