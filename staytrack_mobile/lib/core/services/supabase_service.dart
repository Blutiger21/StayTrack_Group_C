import 'dart:io';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mime/mime.dart';
import 'package:path/path.dart' as p;
import 'package:supabase_flutter/supabase_flutter.dart';
import 'package:staytrack_mobile/core/constants/app_constants.dart';
import 'package:staytrack_mobile/core/models/announcement.dart';
import 'package:staytrack_mobile/core/models/maintenance_request.dart';
import 'package:staytrack_mobile/core/models/payment.dart';
import 'package:staytrack_mobile/core/models/tenant.dart';
import 'package:staytrack_mobile/core/models/user_profile.dart';

// ── Provider ──────────────────────────────────────────────────────────────────
final supabaseServiceProvider = Provider<SupabaseService>((ref) {
  return SupabaseService(Supabase.instance.client);
});

// ── Service ───────────────────────────────────────────────────────────────────
class SupabaseService {
  final SupabaseClient _client;
  SupabaseService(this._client);

  // ── Auth ────────────────────────────────────────────────────────────────────

  /// Send a one-time password to [email] via Supabase Auth.
  Future<void> sendOtp(String email) async {
    await _client.auth.signInWithOtp(
      email: email,
      shouldCreateUser: false, // student must already be added by admin
    );
  }

  /// Verify the [otp] token that was emailed.
  Future<AuthResponse> verifyOtp(String email, String otp) async {
    return _client.auth.verifyOTP(
      email: email,
      token: otp,
      type: OtpType.email,
    );
  }

  /// Sign out the current user.
  Future<void> signOut() => _client.auth.signOut();

  /// Stream of auth state changes – used to drive navigation guards.
  Stream<AuthState> get authStateChanges => _client.auth.onAuthStateChange;

  User? get currentUser => _client.auth.currentUser;

  // ── Profile ─────────────────────────────────────────────────────────────────

  Future<UserProfile?> getProfile(String userId) async {
    final data = await _client
        .from('profiles')
        .select()
        .eq('id', userId)
        .maybeSingle();
    return data != null ? UserProfile.fromMap(data) : null;
  }

  /// Creates a profile row if it doesn't already exist (called after OTP verify).
  Future<void> upsertProfile(UserProfile profile) async {
    await _client.from('profiles').upsert(profile.toMap());
  }

  // ── Tenant ──────────────────────────────────────────────────────────────────

  /// Fetch the tenant record for the authenticated user.
  /// The web app stores the Supabase Auth user UUID in the [user_id] column.
  Future<Tenant?> getTenantByUserId(String userId) async {
    final data = await _client
        .from('tenant')
        .select('*, room:room_id(*)')   // join room info
        .eq('user_id', userId)
        .eq('status', 'active')
        .maybeSingle();
    return data != null ? Tenant.fromMap(data) : null;
  }

  // ── Payments ────────────────────────────────────────────────────────────────

  Future<List<Payment>> getPaymentsByTenant(int tenantId) async {
    final data = await _client
        .from('payment')
        .select()
        .eq('tenant_id', tenantId)
        .order('payment_date', ascending: false);
    return (data as List).map((e) => Payment.fromMap(e)).toList();
  }

  /// Upload proof of payment file to Supabase Storage and insert a payment row.
  Future<void> submitPaymentProof({
    required int tenantId,
    required double amount,
    required String periodMonth, // e.g. "2025-03-01"
    required String reference,
    required File file,
  }) async {
    // 1. Upload file with unique timestamp to prevent collisions
    final ext = p.extension(file.path);
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    final cleanMonth = periodMonth.replaceAll('-', '');
    final fileName = '${tenantId}_${cleanMonth}_$timestamp$ext';
    final mimeType = lookupMimeType(file.path) ?? 'application/octet-stream';

    String targetBucket = AppConstants.paymentProofsBucket; // 'proof-of-payment'
    try {
      await _client.storage
          .from(targetBucket)
          .upload(
            fileName,
            file,
            fileOptions: FileOptions(contentType: mimeType, upsert: true),
          );
    } on StorageException catch (e) {
      // Fallback: if 'proof-of-payment' is not found, try 'payment-proofs'
      if ((e.statusCode == '404' || e.message.contains('Bucket not found')) &&
          targetBucket != 'payment-proofs') {
        targetBucket = 'payment-proofs';
        await _client.storage
            .from(targetBucket)
            .upload(
              fileName,
              file,
              fileOptions: FileOptions(contentType: mimeType, upsert: true),
            );
      } else {
        rethrow;
      }
    }

    // 2. Insert payment row matching the DB schema exactly.
    // Storing relative path 'proof-of-payment/fileName' ensures ASP.NET PaymentService
    // can generate secure signed URLs for management verification.
    final proofPath = '$targetBucket/$fileName';
    final paymentDate = DateTime.now().toIso8601String().split('T').first; // 'yyyy-MM-dd'

    await _client.from('payment').insert({
      'tenant_id': tenantId,
      'amount': amount,
      'payment_date': paymentDate,
      'period_month': periodMonth,
      'reference': reference,
      'status': 'pending',
      'proof_url': proofPath,
    });
  }

  // ── Maintenance ─────────────────────────────────────────────────────────────

  /// Returns ALL maintenance requests (community feed).
  Future<List<MaintenanceRequest>> getAllMaintenanceRequests() async {
    final data = await _client
        .from('maintenance_request')
        .select()
        .order('date_submitted', ascending: false);
    return (data as List).map((e) => MaintenanceRequest.fromMap(e)).toList();
  }

  /// Returns only THIS tenant's maintenance requests.
  Future<List<MaintenanceRequest>> getMaintenanceByTenant(int tenantId) async {
    final data = await _client
        .from('maintenance_request')
        .select()
        .eq('tenant_id', tenantId)
        .order('date_submitted', ascending: false);
    return (data as List).map((e) => MaintenanceRequest.fromMap(e)).toList();
  }

  Future<void> submitMaintenanceRequest({
    required int tenantId,
    required String title,
    required String description,
    File? photo,
  }) async {
    String? photoUrl;

    if (photo != null) {
      final ext = p.extension(photo.path);
      final fileName = '${tenantId}_${DateTime.now().millisecondsSinceEpoch}$ext';
      final mimeType = lookupMimeType(photo.path) ?? 'image/jpeg';

      await _client.storage
          .from(AppConstants.maintenancePhotosBucket)
          .upload(
            fileName,
            photo,
            fileOptions: FileOptions(contentType: mimeType, upsert: true),
          );

      photoUrl = '${AppConstants.maintenancePhotosBucket}/$fileName';
    }

    // Insert only columns that exist in the DB schema
    await _client.from('maintenance_request').insert({
      'tenant_id': tenantId,
      'title': title,
      'description': description,
      'status': 'pending',
      'date_submitted': DateTime.now().toIso8601String(),
      if (photoUrl != null) 'photo_url': photoUrl,
    });
  }

  // ── Announcements ───────────────────────────────────────────────────────────

  Future<List<Announcement>> getAnnouncements() async {
    final data = await _client
        .from('announcement')
        .select('*, profiles:created_by(full_name)')
        .order('created_at', ascending: false);
    return (data as List).map((e) => Announcement.fromMap(e)).toList();
  }
}
