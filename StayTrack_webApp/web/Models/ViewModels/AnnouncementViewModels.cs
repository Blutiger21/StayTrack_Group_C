using System.ComponentModel.DataAnnotations;
using StayTrack.Models.Entities;

namespace StayTrack.Models.ViewModels;

/// <summary>
/// Form view model for posting a new broadcast announcement.
/// </summary>
public class AnnouncementFormViewModel
{
    [Required(ErrorMessage = "Announcement title is required.")]
    [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
    [Display(Name = "Announcement Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Announcement message is required.")]
    [StringLength(3000, ErrorMessage = "Message cannot exceed 3000 characters.")]
    [Display(Name = "Announcement Message / Notice")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// View model for the Announcements page combining the post form and previous announcements list.
/// </summary>
public class AnnouncementsPageViewModel
{
    public AnnouncementFormViewModel Form { get; set; } = new();

    public List<Announcement> Announcements { get; set; } = new();
}
