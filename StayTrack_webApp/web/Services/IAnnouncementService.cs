using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service interface for management broadcast announcements.
/// All queries carry the signed-in manager's JWT token for Supabase RLS.
/// </summary>
public interface IAnnouncementService
{
    /// <summary>
    /// Retrieves all announcements ordered with newest first.
    /// </summary>
    Task<List<Announcement>> GetAnnouncementsAsync();

    /// <summary>
    /// Retrieves the single most recent announcement for the dashboard overview.
    /// </summary>
    Task<Announcement?> GetLatestAnnouncementAsync();

    /// <summary>
    /// Creates a new broadcast announcement.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> CreateAnnouncementAsync(string title, string message, Guid createdByUserId);

    /// <summary>
    /// Deletes an announcement by its ID.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> DeleteAnnouncementAsync(int announcementId);
}
