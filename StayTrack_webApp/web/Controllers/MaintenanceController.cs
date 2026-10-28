using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;
using StayTrack.Models.ViewModels;
using StayTrack.Services;

namespace StayTrack.Controllers;

/// <summary>
/// Controller for Accommodation Management to oversee maintenance requests,
/// assign repair technicians, and track ticket completion.
/// Restricted strictly to the Management role.
/// </summary>
[Authorize(Roles = AppRoles.Management)]
public class MaintenanceController : Controller
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(IMaintenanceService maintenanceService, ILogger<MaintenanceController> logger)
    {
        _maintenanceService = maintenanceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string filter = "all", string? search = null)
    {
        var requests = await _maintenanceService.GetAllRequestsAsync(filter, search);
        var (total, pending, assigned, completed) = await _maintenanceService.GetRequestCountsAsync();

        var model = new MaintenanceListViewModel
        {
            Requests = requests,
            StatusFilter = filter.ToLowerInvariant(),
            SearchQuery = search,
            TotalCount = total,
            PendingCount = pending,
            AssignedCount = assigned,
            CompletedCount = completed
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _maintenanceService.GetRequestByIdAsync(id);
        if (request == null)
        {
            TempData["Error"] = "Maintenance request record not found.";
            return RedirectToAction(nameof(Index));
        }

        var technicians = await _maintenanceService.GetTechniciansAsync();
        var signedPhotoUrl = await _maintenanceService.GetPhotoSignedUrlAsync(request.PhotoUrl);

        var model = new MaintenanceDetailViewModel
        {
            Request = request,
            SignedPhotoUrl = signedPhotoUrl,
            AvailableTechnicians = technicians,
            SelectedTechnicianId = request.TechnicianId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTechnician(int id, int technicianId)
    {
        var (success, errorMessage) = await _maintenanceService.AssignTechnicianAsync(id, technicianId);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to assign technician.";
        }
        else
        {
            TempData["Success"] = "Technician successfully assigned. Request status set to 'Assigned'.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var (success, errorMessage) = await _maintenanceService.UpdateStatusAsync(id, status);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to update request status.";
        }
        else
        {
            TempData["Success"] = $"Maintenance request status updated to '{status}'.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
