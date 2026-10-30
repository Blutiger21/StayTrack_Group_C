using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for the main Tenant Management page displaying tenants, properties, and rooms.
/// </summary>
public class TenantListViewModel
{
    public List<Tenant> Tenants { get; set; } = new();

    public List<Property> Properties { get; set; } = new();

    public List<Room> Rooms { get; set; } = new();

    public string? SearchQuery { get; set; }

    public string ActiveTab { get; set; } = "tenants"; // "tenants" or "rooms"
}
