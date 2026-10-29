using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;
using StayTrack.Models.ViewModels;
using StayTrack.Services;

namespace StayTrack.Controllers;

/// <summary>
/// Controller managing staff authentication via Supabase Email OTP.
/// Handles OTP request, OTP code verification, role-based redirects
/// (Management to Dashboard, Technician to My Assigned Requests),
/// access denial for student or unauthorized accounts, and secure sign-out.
/// </summary>
public class AccountController : Controller
{
    private readonly ISupabaseAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ISupabaseAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(AppRoles.Management))
            {
                return RedirectToAction("Index", "Dashboard");
            }
            if (User.IsInRole(AppRoles.Technician))
            {
                return RedirectToAction("Index", "Technician");
            }

            return RedirectToAction("AccessDenied");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOtp(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Login", model);
        }

        var (success, errorMessage) = await _authService.SendOtpAsync(model.Email);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to send verification code. Please check your email.");
            return View("Login", model);
        }

        TempData["Success"] = $"A 6-digit one-time passcode has been sent to {model.Email}. Please check your inbox.";
        return RedirectToAction("VerifyOtp", new { email = model.Email, returnUrl = model.ReturnUrl });
    }

    [HttpGet]
    public IActionResult VerifyOtp(string? email, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Please enter your staff email address first.";
            return RedirectToAction("Login");
        }

        return View(new VerifyOtpViewModel
        {
            Email = email.Trim(),
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (verified, tokenData, errorMessage) = await _authService.VerifyOtpAsync(model.Email, model.Token);

        if (!verified || tokenData == null || tokenData.User == null)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Invalid or expired verification code.");
            return View(model);
        }

        // Fetch user profile and associated role from Supabase PostgreSQL
        var profile = await _authService.GetUserProfileAsync(tokenData.AccessToken, tokenData.User.Id);

        if (profile == null)
        {
            _logger.LogWarning("Login succeeded in auth but profile record not found for {UserId}", tokenData.User.Id);
            ModelState.AddModelError(string.Empty, "Authenticated successfully, but no matching staff profile was found. Please contact your system administrator.");
            return View(model);
        }

        if (!profile.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This staff account has been deactivated. Please contact your system administrator.");
            return View(model);
        }

        var roleName = profile.Role?.RoleName?.ToLowerInvariant() ?? string.Empty;

        // Authorize strictly for management and technician roles
        if (roleName == AppRoles.Management)
        {
            await _authService.SignInAsync(profile, tokenData);
            TempData["Success"] = $"Welcome back, {profile.FullName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }
        else if (roleName == AppRoles.Technician)
        {
            await _authService.SignInAsync(profile, tokenData);
            TempData["Success"] = $"Welcome back, {profile.FullName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Technician");
        }
        else
        {
            // Student or unrecognized role: sign out and send to AccessDenied
            await _authService.SignOutAsync();
            return RedirectToAction("AccessDenied");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync();
        TempData["Success"] = "You have been logged out successfully.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
