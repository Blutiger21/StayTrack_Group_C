namespace StayTrack.Models;

/// <summary>
/// Constants representing user role names in the StayTrack system.
/// Matches role_name values in the Supabase 'role' table.
/// </summary>
public static class AppRoles
{
    public const string Management = "management";
    public const string Technician = "technician";
    public const string Student = "student";
}
