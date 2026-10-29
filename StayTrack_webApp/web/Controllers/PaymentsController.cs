using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;
using StayTrack.Models.Entities;
using StayTrack.Models.ViewModels;
using StayTrack.Services;

namespace StayTrack.Controllers;

/// <summary>
/// Controller for managing student accommodation rent payments.
/// Handles payment listing with status tabs, viewing proofs of payment via signed URLs,
/// approving/rejecting payments, and manually recording payments.
/// Restricted strictly to the Management role.
/// </summary>
[Authorize(Roles = AppRoles.Management)]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    private Guid CurrentUserId
    {
        get
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "all", string? search = null)
    {
        var payments = await _paymentService.GetPaymentsAsync(tab, search);
        var (total, pending, approved, rejected) = await _paymentService.GetPaymentCountsAsync();

        var model = new PaymentListViewModel
        {
            Payments = payments,
            ActiveTab = tab.ToLowerInvariant(),
            SearchQuery = search,
            AllCount = total,
            PendingCount = pending,
            ApprovedCount = approved,
            RejectedCount = rejected
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
        {
            TempData["Error"] = "Payment record not found.";
            return RedirectToAction(nameof(Index));
        }

        string? signedUrl = null;
        var isImage = false;
        var isPdf = false;

        if (!string.IsNullOrWhiteSpace(payment.ProofUrl))
        {
            signedUrl = await _paymentService.GetProofSignedUrlAsync(payment.ProofUrl);

            var cleanExtPath = payment.ProofUrl.Split('?')[0].ToLowerInvariant();
            var ext = Path.GetExtension(cleanExtPath);
            if (ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif")
            {
                isImage = true;
            }
            else if (ext is ".pdf")
            {
                isPdf = true;
            }
            else
            {
                // Default to image if extension not explicitly pdf
                isImage = true;
            }
        }

        var model = new PaymentDetailViewModel
        {
            Payment = payment,
            SignedProofUrl = signedUrl,
            IsImage = isImage,
            IsPdf = isPdf
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var (success, errorMessage) = await _paymentService.UpdatePaymentStatusAsync(id, "approved", CurrentUserId);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to approve payment.";
        }
        else
        {
            TempData["Success"] = "Payment has been successfully approved.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var (success, errorMessage) = await _paymentService.UpdatePaymentStatusAsync(id, "rejected", CurrentUserId);
        if (!success)
        {
            TempData["Error"] = errorMessage ?? "Failed to reject payment.";
        }
        else
        {
            TempData["Warning"] = "Payment has been marked as rejected.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Record(string? reference = null)
    {
        var model = new RecordPaymentViewModel();

        if (!string.IsNullOrWhiteSpace(reference))
        {
            model.PaymentReference = reference.Trim();
            var tenant = await _paymentService.FindTenantByReferenceAsync(reference);
            if (tenant != null)
            {
                model.MatchedTenant = tenant;
                model.TenantId = tenant.TenantId;
                model.Amount = tenant.RentalAmount;
            }
            else
            {
                ModelState.AddModelError("PaymentReference", $"No active tenant found matching reference '{reference}'.");
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(RecordPaymentViewModel model, string? action)
    {
        // If clicking "Lookup" or if tenant is not confirmed yet
        if (action == "lookup" || model.TenantId == null)
        {
            if (string.IsNullOrWhiteSpace(model.PaymentReference))
            {
                ModelState.AddModelError("PaymentReference", "Please enter a payment reference to look up.");
                return View(model);
            }

            var tenant = await _paymentService.FindTenantByReferenceAsync(model.PaymentReference);
            if (tenant == null)
            {
                ModelState.AddModelError("PaymentReference", $"No tenant found matching reference '{model.PaymentReference}'.");
                return View(model);
            }

            model.MatchedTenant = tenant;
            model.TenantId = tenant.TenantId;
            model.Amount = tenant.RentalAmount;
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            if (model.TenantId.HasValue)
            {
                model.MatchedTenant = await _paymentService.FindTenantByReferenceAsync(model.PaymentReference);
            }
            return View(model);
        }

        var payment = new Payment
        {
            TenantId = model.TenantId.Value,
            Amount = model.Amount,
            PaymentDate = model.PaymentDate,
            PeriodMonth = new DateTime(model.PeriodMonth.Year, model.PeriodMonth.Month, 1),
            Reference = string.IsNullOrWhiteSpace(model.CustomReference) 
                ? model.PaymentReference.Trim() 
                : $"{model.PaymentReference.Trim()} - {model.CustomReference.Trim()}",
            Status = "approved"
        };

        var (success, errorMessage) = await _paymentService.RecordPaymentAsync(payment, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to save payment.");
            model.MatchedTenant = await _paymentService.FindTenantByReferenceAsync(model.PaymentReference);
            return View(model);
        }

        TempData["Success"] = $"Payment of R {payment.Amount:N2} for tenant reference {model.PaymentReference} successfully recorded and approved.";
        return RedirectToAction(nameof(Index), new { tab = "approved" });
    }
}