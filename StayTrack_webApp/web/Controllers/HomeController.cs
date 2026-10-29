using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;

namespace StayTrack.Controllers;

/// <summary>
/// Landing and routing controller for StayTrack.
/// Routes signed-in users directly to their role-specific landing page
/// (Management to Dashboard, Technician to Assigned Requests)
/// or displays a portal overview for guests.
/// </summary>
public class HomeController : Controller
{
    public IActionResult Index()
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

            return RedirectToAction("AccessDenied", "Account");
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
