using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;
using StayTrack.Models.Entities;
using StayTrack.Models.ViewModels;
using StayTrack.Services;

namespace StayTrack.Controllers;

/// <summary>
/// Controller for student accommodation tenant management, room allocations,
/// and property management. Restricted strictly to the Management role.
/// </summary>
[Authorize(Roles = AppRoles.Management)]
public class TenantsController : Controller
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ITenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string tab = "tenants")
    {
        var tenants = await _tenantService.GetTenantsAsync(search);
        var properties = await _tenantService.GetPropertiesAsync();
        var rooms = await _tenantService.GetRoomsAsync();

        var model = new TenantListViewModel
        {
            Tenants = tenants,
            Properties = properties,
            Rooms = rooms,
            SearchQuery = search,
            ActiveTab = tab
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var unassignedStudents = await _tenantService.GetUnassignedStudentProfilesAsync();
        var rooms = await _tenantService.GetRoomsAsync();
        var suggestedRef = await _tenantService.GeneratePaymentReferenceAsync();

        var model = new TenantFormViewModel
        {
            PaymentReference = suggestedRef,
            LeaseStart = DateTime.Today,
            LeaseEnd = DateTime.Today.AddMonths(12),
            Status = "active",
            AvailableStudents = unassignedStudents,
            AvailableRooms = rooms
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenantFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableStudents = await _tenantService.GetUnassignedStudentProfilesAsync();
            model.AvailableRooms = await _tenantService.GetRoomsAsync();
            return View(model);
        }

        var tenant = new Tenant
        {
            UserId = model.UserId,
            RoomId = model.RoomId,
            StudentNumber = model.StudentNumber.Trim(),
            PaymentReference = model.PaymentReference.Trim().ToUpperInvariant(),
            RentalAmount = model.RentalAmount,
            LeaseStart = model.LeaseStart,
            LeaseEnd = model.LeaseEnd,
            Status = model.Status
        };

        var (success, errorMessage) = await _tenantService.CreateTenantAsync(tenant);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to save tenant lease.");
            model.AvailableStudents = await _tenantService.GetUnassignedStudentProfilesAsync();
            model.AvailableRooms = await _tenantService.GetRoomsAsync();
            return View(model);
        }

        TempData["Success"] = $"Tenant lease created successfully with Payment Reference {tenant.PaymentReference}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tenant = await _tenantService.GetTenantByIdAsync(id);
        if (tenant == null)
        {
            TempData["Error"] = "Tenant record not found.";
            return RedirectToAction(nameof(Index));
        }

        var rooms = await _tenantService.GetRoomsAsync();

        var model = new TenantFormViewModel
        {
            TenantId = tenant.TenantId,
            UserId = tenant.UserId,
            RoomId = tenant.RoomId,
            StudentNumber = tenant.StudentNumber,
            PaymentReference = tenant.PaymentReference,
            RentalAmount = tenant.RentalAmount,
            LeaseStart = tenant.LeaseStart,
            LeaseEnd = tenant.LeaseEnd,
            Status = tenant.Status,
            ExistingStudentName = tenant.Profile?.FullName,
            ExistingStudentEmail = tenant.Profile?.Email,
            AvailableRooms = rooms
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TenantFormViewModel model)
    {
        if (id != model.TenantId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRooms = await _tenantService.GetRoomsAsync();
            return View(model);
        }

        var tenant = new Tenant
        {
            TenantId = model.TenantId.Value,
            UserId = model.UserId,
            RoomId = model.RoomId,
            StudentNumber = model.StudentNumber.Trim(),
            PaymentReference = model.PaymentReference.Trim().ToUpperInvariant(),
            RentalAmount = model.RentalAmount,
            LeaseStart = model.LeaseStart,
            LeaseEnd = model.LeaseEnd,
            Status = model.Status
        };

        var (success, errorMessage) = await _tenantService.UpdateTenantAsync(tenant);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to update tenant lease.");
            model.AvailableRooms = await _tenantService.GetRoomsAsync();
            return View(model);
        }

        TempData["Success"] = "Tenant lease updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var (success, errorMessage) = await _tenantService.ArchiveTenantAsync(id);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to archive tenant.";
        }
        else
        {
            TempData["Success"] = "Tenant lease successfully archived (marked inactive).";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(RoomFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill in all room details properly.";
            return RedirectToAction(nameof(Index), new { tab = "rooms" });
        }

        var room = new Room
        {
            PropertyId = model.PropertyId,
            RoomNumber = model.RoomNumber.Trim(),
            RoomType = model.RoomType.Trim(),
            Status = model.Status
        };

        var (success, errorMessage) = await _tenantService.CreateRoomAsync(room);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to add room.";
        }
        else
        {
            TempData["Success"] = $"Room {room.RoomNumber} successfully added.";
        }

        return RedirectToAction(nameof(Index), new { tab = "rooms" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProperty(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please provide both the property name and address.";
            return RedirectToAction(nameof(Index), new { tab = "rooms" });
        }

        var property = new Property
        {
            Name = model.Name.Trim(),
            Address = model.Address.Trim()
        };

        var (success, errorMessage) = await _tenantService.CreatePropertyAsync(property);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to add property.";
        }
        else
        {
            TempData["Success"] = $"Property '{property.Name}' successfully created.";
        }

        return RedirectToAction(nameof(Index), new { tab = "rooms" });
    }
}
