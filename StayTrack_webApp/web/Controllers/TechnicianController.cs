using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;
using StayTrack.Models.ViewModels;
using StayTrack.Services;

namespace StayTrack.Controllers;

/// <summary>
/// Controller for Maintenance Technicians to inspect and complete their assigned repair tasks.
/// Enforces strict RLS so technicians only see and update tickets assigned to them.
/// Restricted strictly to the Technician role.
/// </summary>
[Authorize(Roles = AppRoles.Technician)]
public class TechnicianController : Controller
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly ILogger<TechnicianController> _logger;

    public TechnicianController(IMaintenanceService maintenanceService, ILogger<TechnicianController> logger)
    {
        _maintenanceService = maintenanceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var requests = await _maintenanceService.GetAssignedRequestsForCurrentTechnicianAsync();
        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _maintenanceService.GetTechnicianRequestByIdAsync(id);
        if (request == null)
        {
            TempData["Error"] = "Maintenance request not found or not assigned to your account.";
            return RedirectToAction(nameof(Index));
        }

        var photoUrl = await _maintenanceService.GetPhotoSignedUrlAsync(request.PhotoUrl);

        var model = new TechnicianRequestDetailViewModel
        {
            Request = request,
            SignedPhotoUrl = photoUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var (success, errorMessage) = await _maintenanceService.MarkCompletedByTechnicianAsync(id);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to mark request as completed.";
        }
        else
        {
            TempData["Success"] = "Maintenance task successfully marked as Completed.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
