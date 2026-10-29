import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:staytrack_mobile/core/models/announcement.dart';
import 'package:staytrack_mobile/core/services/supabase_service.dart';

final announcementsProvider = FutureProvider<List<Announcement>>((ref) async {
  return ref.watch(supabaseServiceProvider).getAnnouncements();
});
