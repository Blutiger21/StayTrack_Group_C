using StayTrack.Models.ViewModels;

namespace StayTrack.Services;

/// <summary>
/// Service interface for generating on-demand financial reconciliation and rent collection reports.
/// Generates live reports directly from database records without storing report tables.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates an on-demand monthly report for the specified month.
    /// Calculates Paid, Pending, and Unpaid status for every active tenant based on verified payments.
    /// </summary>
    Task<MonthlyReportViewModel> GenerateMonthlyReportAsync(DateTime month);

    /// <summary>
    /// Generates CSV bytes for the provided monthly report.
    /// </summary>
    byte[] GenerateCsv(MonthlyReportViewModel report);
}
