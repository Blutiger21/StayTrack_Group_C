using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for the Management Maintenance request dashboard with status filtering and counts.
/// </summary>
public class MaintenanceListViewModel
{
    public List<MaintenanceRequest> Requests { get; set; } = new();

    public string StatusFilter { get; set; } = "all"; // "all", "pending", "assigned", "completed"

    public string? SearchQuery { get; set; }

    public int TotalCount { get; set; }

    public int PendingCount { get; set; }

    public int AssignedCount { get; set; }

    public int CompletedCount { get; set; }
}
