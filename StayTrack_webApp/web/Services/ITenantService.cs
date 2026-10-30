using StayTrack.Models.Entities;

namespace StayTrack.Services;

/// <summary>
/// Service interface for Tenant, Property, and Room operations.
/// Interacts with Supabase REST API carrying the authenticated manager's JWT token.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Retrieves all student tenants with their joined profile, room, and property details.
    /// Optionally filters by search query across student name, student number, room, or payment reference.
    /// </summary>
    Task<List<Tenant>> GetTenantsAsync(string? searchQuery = null);

    /// <summary>
    /// Retrieves a single tenant by their tenant_id.
    /// </summary>
    Task<Tenant?> GetTenantByIdAsync(int tenantId);

    /// <summary>
    /// Retrieves all active student profiles that do not currently have an active lease record.
    /// </summary>
    Task<List<Profile>> GetUnassignedStudentProfilesAsync();

    /// <summary>
    /// Retrieves all accommodation properties / residences.
    /// </summary>
    Task<List<Property>> GetPropertiesAsync();

    /// <summary>
    /// Retrieves all rooms with joined property details.
    /// </summary>
    Task<List<Room>> GetRoomsAsync();

    /// <summary>
    /// Suggests a new unique payment reference in the format "STAY-XXXX".
    /// </summary>
    Task<string> GeneratePaymentReferenceAsync();

    /// <summary>
    /// Creates a new tenant lease record in Supabase and updates the assigned room to occupied.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> CreateTenantAsync(Tenant tenant);

    /// <summary>
    /// Updates an existing tenant lease record.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> UpdateTenantAsync(Tenant tenant);

    /// <summary>
    /// Archives a tenant lease record by setting its status to inactive and freeing the room.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> ArchiveTenantAsync(int tenantId);

    /// <summary>
    /// Adds a new room to a property.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> CreateRoomAsync(Room room);

    /// <summary>
    /// Adds a new property / residence building.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> CreatePropertyAsync(Property property);
}
