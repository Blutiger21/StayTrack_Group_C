using System.ComponentModel.DataAnnotations;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// View model for entering and submitting the one-time passcode (OTP).
/// </summary>
public class VerifyOtpViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the verification code sent to your email.")]
    [StringLength(10, MinimumLength = 4, ErrorMessage = "Verification code must be between 4 and 10 characters.")]
    [Display(Name = "One-Time Passcode (OTP)")]
    public string Token { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
