using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for the Payments list page with tabs (All, Pending, Approved, Rejected) and counts.
/// </summary>
public class PaymentListViewModel
{
    public List<Payment> Payments { get; set; } = new();

    public string ActiveTab { get; set; } = "all"; // "all", "pending", "approved", "rejected"

    public string? SearchQuery { get; set; }

    public int AllCount { get; set; }

    public int PendingCount { get; set; }

    public int ApprovedCount { get; set; }

    public int RejectedCount { get; set; }
}
