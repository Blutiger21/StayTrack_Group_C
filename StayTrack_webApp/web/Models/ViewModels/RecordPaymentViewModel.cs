using System.ComponentModel.DataAnnotations;
using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for recording manual rent payments (e.g. bank transfer or cash) directly by management.
/// </summary>
public class RecordPaymentViewModel
{
    [Required(ErrorMessage = "Please enter the tenant payment reference.")]
    [Display(Name = "Tenant Payment Reference")]
    public string PaymentReference { get; set; } = string.Empty;

    public int? TenantId { get; set; }

    public Tenant? MatchedTenant { get; set; }

    public bool TenantConfirmed => MatchedTenant != null;

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, 1000000, ErrorMessage = "Payment amount must be greater than zero.")]
    [Display(Name = "Amount (R)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Payment date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Period month is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Month Paid For")]
    public DateTime PeriodMonth { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [Display(Name = "Receipt / Bank Reference Note")]
    public string? CustomReference { get; set; }
}
