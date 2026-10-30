using System.ComponentModel.DataAnnotations;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for the initial email submission on the OTP login page.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your staff email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Staff Email Address")]
    public string Email { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
