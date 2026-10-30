using System.ComponentModel.DataAnnotations;
using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for adding a new room to a property.
/// </summary>
public class RoomFormViewModel
{
    [Required(ErrorMessage = "Please select a property.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid property.")]
    [Display(Name = "Property / Residence")]
    public int PropertyId { get; set; }

    [Required(ErrorMessage = "Room number is required.")]
    [StringLength(20, ErrorMessage = "Room number cannot exceed 20 characters.")]
    [Display(Name = "Room Number (e.g. 101, B2)")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room type is required.")]
    [StringLength(50, ErrorMessage = "Room type cannot exceed 50 characters.")]
    [Display(Name = "Room Type")]
    public string RoomType { get; set; } = "Single";

    [Required]
    [Display(Name = "Initial Status")]
    public string Status { get; set; } = "vacant";

    public List<Property> AvailableProperties { get; set; } = new();
}
