using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service interface for Maintenance Request lifecycle, technician assignments,
/// and maintenance photo signed URLs. Strictly enforces role RLS boundaries.
/// </summary>
public interface IMaintenanceService
{
    // --- Management Operations ---

    /// <summary>
    /// Retrieves all maintenance requests with joined tenant, room, and assigned technician details.
    /// Optionally filtered by status ('all', 'pending', 'assigned', 'completed') and search query.
    /// </summary>
    Task<List<MaintenanceRequest>> GetAllRequestsAsync(string? statusFilter = null, string? search = null);

    /// <summary>
    /// Retrieves full details of a single maintenance request for management inspection.
    /// </summary>
    Task<MaintenanceRequest?> GetRequestByIdAsync(int requestId);

    /// <summary>
    /// Retrieves all registered technicians with joined profile details.
    /// </summary>
    Task<List<Technician>> GetTechniciansAsync();

    /// <summary>
    /// Assigns a technician to a maintenance request, updating status to 'assigned'.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> AssignTechnicianAsync(int requestId, int technicianId);

    /// <summary>
    /// Updates the status of a request ('pending', 'assigned', or 'completed').
    /// If completed, sets date_completed.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> UpdateStatusAsync(int requestId, string status, DateTime? dateCompleted = null);

    /// <summary>
    /// Returns counts of total, pending, assigned, and completed maintenance requests.
    /// </summary>
    Task<(int Total, int Pending, int Assigned, int Completed)> GetRequestCountsAsync();

    // --- Technician Operations ---

    /// <summary>
    /// Resolves the technician_id for the signed-in user.
    /// </summary>
    Task<int?> GetCurrentTechnicianIdAsync();

    /// <summary>
    /// Retrieves requests assigned to the signed-in technician.
    /// Does not query tenant data to respect PostgreSQL RLS technician permissions.
    /// </summary>
    Task<List<MaintenanceRequest>> GetAssignedRequestsForCurrentTechnicianAsync();

    /// <summary>
    /// Retrieves a single request assigned to the current technician.
    /// </summary>
    Task<MaintenanceRequest?> GetTechnicianRequestByIdAsync(int requestId);

    /// <summary>
    /// Marks a request as completed by the assigned technician, setting status to 'completed' and date_completed.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> MarkCompletedByTechnicianAsync(int requestId);

    // --- Shared Operations ---

    /// <summary>
    /// Generates a short-lived signed URL for a private maintenance photo from the 'maintenance-photos' storage bucket.
    /// </summary>
    Task<string?> GetPhotoSignedUrlAsync(string? photoUrlPath);
}
