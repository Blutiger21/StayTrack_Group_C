using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for management inspecting a maintenance request, assigning a technician, or changing status.
/// </summary>
public class MaintenanceDetailViewModel
{
    public MaintenanceRequest Request { get; set; } = new();

    public string? SignedPhotoUrl { get; set; }

    public bool HasPhoto => !string.IsNullOrWhiteSpace(SignedPhotoUrl);

    public List<Technician> AvailableTechnicians { get; set; } = new();

    public int? SelectedTechnicianId { get; set; }
}

/// <summary>
/// View model for a technician inspecting an assigned maintenance request.
/// </summary>
public class TechnicianRequestDetailViewModel
{
    public MaintenanceRequest Request { get; set; } = new();

    public string? SignedPhotoUrl { get; set; }

    public bool HasPhoto => !string.IsNullOrWhiteSpace(SignedPhotoUrl);
}
