using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for the Payment Detail page showing all payment attributes and verified proof URL.
/// </summary>
public class PaymentDetailViewModel
{
    public Payment Payment { get; set; } = new();

    public string? SignedProofUrl { get; set; }

    public bool HasProof => !string.IsNullOrWhiteSpace(SignedProofUrl);

    public bool IsImage { get; set; }

    public bool IsPdf { get; set; }
}
