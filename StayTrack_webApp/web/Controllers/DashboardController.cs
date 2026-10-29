using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;
using StayTrack.Models.ViewModels;
using StayTrack.Services;

namespace StayTrack.Controllers;

/// <summary>
/// Executive Management Dashboard controller.
/// Consolidates live metrics and short lists for:
/// - Payments pending verification
/// - Active tenants unpaid for the current month
/// - Outstanding maintenance requests (pending or assigned)
/// - The latest announcement
/// Includes direct links to each operational area.
/// Restricted strictly to the Management role.
/// </summary>
[Authorize(Roles = AppRoles.Management)]
public class DashboardController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IReportService _reportService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IAnnouncementService _announcementService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IPaymentService paymentService,
        IReportService reportService,
        IMaintenanceService maintenanceService,
        IAnnouncementService announcementService,
        ILogger<DashboardController> logger)
    {
        _paymentService = paymentService;
        _reportService = reportService;
        _maintenanceService = maintenanceService;
        _announcementService = announcementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // 1. Pending Payments
        var pendingPayments = await _paymentService.GetPaymentsAsync("pending");

        // 2. Active Tenants Unpaid for the current month
        var monthlyReport = await _reportService.GenerateMonthlyReportAsync(DateTime.Today);
        var unpaidItems = monthlyReport.Items.Where(i => i.Status != "Paid").ToList();

        // 3. Outstanding Maintenance Requests (pending or assigned)
        var allRequests = await _maintenanceService.GetAllRequestsAsync("all");
        var outstandingRequests = allRequests.Where(r => r.Status == "pending" || r.Status == "assigned").ToList();

        // 4. Latest Announcement
        var latestAnnouncement = await _announcementService.GetLatestAnnouncementAsync();

        var model = new DashboardViewModel
        {
            PendingPaymentsCount = pendingPayments.Count,
            PendingPayments = pendingPayments.Take(5).ToList(),

            UnpaidTenantsCount = unpaidItems.Count,
            UnpaidTenants = unpaidItems.Take(5).ToList(),

            OutstandingRequestsCount = outstandingRequests.Count,
            OutstandingRequests = outstandingRequests.Take(5).ToList(),

            LatestAnnouncement = latestAnnouncement,

            ActiveTenantsCount = monthlyReport.TotalTenants,
            MonthlyExpectedRent = monthlyReport.TotalExpected,
            MonthlyCollectedRent = monthlyReport.TotalCollected
        };

        return View(model);
    }
}
