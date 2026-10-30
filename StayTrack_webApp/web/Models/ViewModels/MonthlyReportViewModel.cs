namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for the on-demand Monthly Financial and Rent Collection Report.
/// </summary>
public class MonthlyReportViewModel
{
    public DateTime SelectedMonth { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    public string MonthString => SelectedMonth.ToString("yyyy-MM");

    public string MonthDisplayName => SelectedMonth.ToString("MMMM yyyy");

    public List<MonthlyTenantReportItem> Items { get; set; } = new();

    public int TotalTenants => Items.Count;

    public int PaidCount => Items.Count(i => i.Status == "Paid");

    public int UnpaidCount => Items.Count(i => i.Status == "Unpaid");

    public int PendingCount => Items.Count(i => i.Status == "Pending");

    public decimal TotalExpected => Items.Sum(i => i.RentalAmount);

    public decimal TotalCollected => Items.Sum(i => i.ApprovedAmount);

    public decimal TotalOutstanding => Math.Max(0, TotalExpected - TotalCollected);
}

public class MonthlyTenantReportItem
{
    public int TenantId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public string PropertyName { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string PaymentReference { get; set; } = string.Empty;

    public decimal RentalAmount { get; set; }

    public decimal ApprovedAmount { get; set; }

    public decimal PendingAmount { get; set; }

    /// <summary>
    /// "Paid" if ApprovedAmount >= RentalAmount,
    /// "Pending" if only a pending payment exists (ApprovedAmount == 0 && PendingAmount > 0),
    /// otherwise "Unpaid".
    /// </summary>
    public string Status { get; set; } = "Unpaid";
}
