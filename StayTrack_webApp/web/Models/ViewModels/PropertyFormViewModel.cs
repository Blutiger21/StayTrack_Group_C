using System.ComponentModel.DataAnnotations;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for adding a new accommodation property / building.
/// </summary>
public class PropertyFormViewModel
{
    [Required(ErrorMessage = "Property name is required.")]
    [StringLength(100, ErrorMessage = "Property name cannot exceed 100 characters.")]
    [Display(Name = "Property Name (e.g. West Campus Residence)")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    [Display(Name = "Physical Address")]
    public string Address { get; set; } = string.Empty;
}
