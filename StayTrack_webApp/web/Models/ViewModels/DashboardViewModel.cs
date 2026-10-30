using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for the executive Management Dashboard.
/// Holds counts and short lists for pending payments, unpaid tenants, open repairs, and latest announcement.
/// </summary>
public class DashboardViewModel
{
    // 1. Payments Pending Verification
    public int PendingPaymentsCount { get; set; }
    public List<Payment> PendingPayments { get; set; } = new();

    // 2. Active Tenants Unpaid for the Current Month
    public int UnpaidTenantsCount { get; set; }
    public List<MonthlyTenantReportItem> UnpaidTenants { get; set; } = new();

    // 3. Outstanding Maintenance Requests (pending or assigned)
    public int OutstandingRequestsCount { get; set; }
    public List<MaintenanceRequest> OutstandingRequests { get; set; } = new();

    // 4. Latest Broadcast Announcement
    public Announcement? LatestAnnouncement { get; set; }

    // Aggregate summary counters
    public int ActiveTenantsCount { get; set; }
    public decimal MonthlyExpectedRent { get; set; }
    public decimal MonthlyCollectedRent { get; set; }
}
