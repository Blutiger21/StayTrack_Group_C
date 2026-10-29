import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:staytrack_mobile/core/models/announcement.dart';
import 'package:staytrack_mobile/core/models/tenant.dart';
import 'package:staytrack_mobile/core/models/user_profile.dart';
import 'package:staytrack_mobile/core/services/supabase_service.dart';
import 'package:staytrack_mobile/features/auth/presentation/providers/auth_provider.dart';

// ── User Profile ──────────────────────────────────────────────────────────────
final profileProvider = FutureProvider<UserProfile?>((ref) async {
  final user = ref.watch(currentUserProvider);
  if (user == null) return null;
  return ref.watch(supabaseServiceProvider).getProfile(user.id);
});

// ── Tenant ────────────────────────────────────────────────────────────────────
final tenantProvider = FutureProvider<Tenant?>((ref) async {
  final user = ref.watch(currentUserProvider);
  if (user == null) return null;
  return ref.watch(supabaseServiceProvider).getTenantByUserId(user.id);
});

// ── Latest announcement ───────────────────────────────────────────────────────
final latestAnnouncementProvider = FutureProvider<Announcement?>((ref) async {
  final announcements =
      await ref.watch(supabaseServiceProvider).getAnnouncements();
  return announcements.isNotEmpty ? announcements.first : null;
});
