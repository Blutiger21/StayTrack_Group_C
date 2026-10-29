using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;
using StayTrack.Models.ViewModels;
using StayTrack.Services;

namespace StayTrack.Controllers;

/// <summary>
/// Controller for Accommodation Management to broadcast announcements to students and staff.
/// Includes creation and chronological history of all announcements.
/// Restricted strictly to the Management role.
/// </summary>
[Authorize(Roles = AppRoles.Management)]
public class AnnouncementsController : Controller
{
    private readonly IAnnouncementService _announcementService;
    private readonly ILogger<AnnouncementsController> _logger;

    public AnnouncementsController(IAnnouncementService announcementService, ILogger<AnnouncementsController> logger)
    {
        _announcementService = announcementService;
        _logger = logger;
    }

    private Guid CurrentUserId
    {
        get
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.Sid)?.Value;
            return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var announcements = await _announcementService.GetAnnouncementsAsync();
        var model = new AnnouncementsPageViewModel
        {
            Announcements = announcements
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] AnnouncementFormViewModel model)
    {
        var title = !string.IsNullOrWhiteSpace(model?.Title)
            ? model.Title
            : (Request.Form["Form.Title"].FirstOrDefault() ?? Request.Form["Title"].FirstOrDefault() ?? string.Empty).Trim();

        var message = !string.IsNullOrWhiteSpace(model?.Message)
            ? model.Message
            : (Request.Form["Form.Message"].FirstOrDefault() ?? Request.Form["Message"].FirstOrDefault() ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            var pageModel = new AnnouncementsPageViewModel
            {
                Form = new AnnouncementFormViewModel { Title = title, Message = message },
                Announcements = await _announcementService.GetAnnouncementsAsync()
            };
            ModelState.AddModelError(string.Empty, "Both title and message are required to broadcast an announcement.");
            return View("Index", pageModel);
        }

        _logger.LogInformation("Submitting announcement: Title='{Title}', UserId='{UserId}'", title, CurrentUserId);

        var (success, errorMessage) = await _announcementService.CreateAnnouncementAsync(
            title, 
            message, 
            CurrentUserId);

        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to post announcement.";
        }
        else
        {
            TempData["Success"] = "Announcement broadcasted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, errorMessage) = await _announcementService.DeleteAnnouncementAsync(id);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to delete announcement.";
        }
        else
        {
            TempData["Success"] = "Announcement removed.";
        }

        return RedirectToAction(nameof(Index));
    }
}
