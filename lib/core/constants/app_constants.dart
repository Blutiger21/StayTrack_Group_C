/// Central place for app-wide string constants.
class AppConstants {
  AppConstants._();

  // ── App info ──────────────────────────────────────────────────────────────
  static const String appName = 'Stay Track';
  static const String appTagline = 'Your home away from home';

  // ── Supabase ──────────────────────────────────────────────────────────────
  // TODO: Replace with your actual Supabase project values from
  //       https://supabase.com/dashboard → Project Settings → API
  static const String supabaseUrl = 'https://vpjctulupuqhwsyasekz.supabase.co';
  static const String supabaseAnonKey =
      'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZwamN0dWx1cHVxaHdzeWFzZWt6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODg0MzAyNDAsImV4cCI6MjEwNDAwNjI0MH0.I7djBVeZ7Yc8IqVuxEzjURixxycPwkT4lv3aM5ofIQ8';

  // ── Storage buckets ───────────────────────────────────────────────────────
  static const String paymentProofsBucket = 'proof-of-payment';
  static const String maintenancePhotosBucket = 'maintenance-photos';

  // ── OTP ───────────────────────────────────────────────────────────────────
  static const int otpLength = 6; // Supabase sends 6-digit OTPs
  static const int otpResendCooldownSeconds = 60;

  // ── Maintenance statuses ──────────────────────────────────────────────────
  static const String statusPending = 'Pending';
  static const String statusInProgress = 'In Progress';
  static const String statusResolved = 'Resolved';

  // ── Payment statuses ──────────────────────────────────────────────────────
  static const String paymentPending = 'Pending';
  static const String paymentVerified = 'Verified';
  static const String paymentRejected = 'Rejected';

  // ── Urgency levels ────────────────────────────────────────────────────────
  static const List<String> urgencyLevels = ['Low', 'Medium', 'High'];
}
