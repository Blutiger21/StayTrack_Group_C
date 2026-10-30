using System.ComponentModel.DataAnnotations;
using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for creating and editing a student tenant lease record.
/// </summary>
public class TenantFormViewModel
{
    public int? TenantId { get; set; }

    [Required(ErrorMessage = "Please select a student profile.")]
    [Display(Name = "Student Profile")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Please select an assigned room.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid room.")]
    [Display(Name = "Room")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Student number is required.")]
    [StringLength(50, ErrorMessage = "Student number cannot exceed 50 characters.")]
    [Display(Name = "Student Number")]
    public string StudentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Payment reference is required.")]
    [StringLength(50, ErrorMessage = "Payment reference cannot exceed 50 characters.")]
    [Display(Name = "Payment Reference (Unique)")]
    public string PaymentReference { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rental amount is required.")]
    [Range(0.01, 1000000, ErrorMessage = "Rental amount must be greater than zero.")]
    [Display(Name = "Monthly Rental Amount")]
    public decimal RentalAmount { get; set; }

    [Required(ErrorMessage = "Lease start date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Lease Start Date")]
    public DateTime LeaseStart { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Lease end date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Lease End Date")]
    public DateTime LeaseEnd { get; set; } = DateTime.Today.AddMonths(12);

    [Required]
    [Display(Name = "Lease Status")]
    public string Status { get; set; } = "active";

    // Read-only info when editing existing tenant
    public string? ExistingStudentName { get; set; }
    public string? ExistingStudentEmail { get; set; }

    // Dropdown choices
    public List<Profile> AvailableStudents { get; set; } = new();
    public List<Room> AvailableRooms { get; set; } = new();
}
