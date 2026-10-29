import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:stay_track/core/models/announcement.dart';
import 'package:stay_track/core/services/supabase_service.dart';

final announcementsProvider = FutureProvider<List<Announcement>>((ref) async {
  return ref.watch(supabaseServiceProvider).getAnnouncements();
});
