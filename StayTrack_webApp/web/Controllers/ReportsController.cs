using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayTrack.Models;
using StayTrack.Services;

namespace StayTrack.Controllers;

/// <summary>
/// Controller for on-demand Monthly Rent Collection and Financial Reports.
/// Generates live reconciliation statistics (Paid, Pending, Unpaid)
/// and exports CSV reports for accommodation management.
/// Restricted strictly to the Management role.
/// </summary>
[Authorize(Roles = AppRoles.Management)]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    private static DateTime ParseMonth(string? monthString)
    {
        if (!string.IsNullOrWhiteSpace(monthString) &&
            DateTime.TryParseExact(monthString, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return new DateTime(parsed.Year, parsed.Month, 1);
        }

        return new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    [HttpGet]
    public async Task<IActionResult> Monthly(string? month)
    {
        var targetMonth = ParseMonth(month);
        var report = await _reportService.GenerateMonthlyReportAsync(targetMonth);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? month)
    {
        var targetMonth = ParseMonth(month);
        var report = await _reportService.GenerateMonthlyReportAsync(targetMonth);
        var csvBytes = _reportService.GenerateCsv(report);

        var fileName = $"StayTrack_Monthly_Report_{report.MonthString}.csv";
        return File(csvBytes, "text/csv", fileName);
    }
}