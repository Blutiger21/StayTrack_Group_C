import 'dart:io';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import 'package:staytrack_mobile/core/models/maintenance_request.dart';
import 'package:staytrack_mobile/core/services/supabase_service.dart';
import 'package:staytrack_mobile/features/home/presentation/providers/home_provider.dart';

// ── This tenant's requests ────────────────────────────────────────────────────
final myMaintenanceRequestsProvider =
    FutureProvider<List<MaintenanceRequest>>((ref) async {
  final tenant = await ref.watch(tenantProvider.future);
  if (tenant == null) return [];
  return ref
      .watch(supabaseServiceProvider)
      .getMaintenanceByTenant(tenant.tenantId);
});

// ── All requests (community feed) ─────────────────────────────────────────────
final maintenanceRequestsProvider =
    FutureProvider<List<MaintenanceRequest>>((ref) async {
  return ref.watch(supabaseServiceProvider).getAllMaintenanceRequests();
});

// ── Submit state ──────────────────────────────────────────────────────────────
enum SubmitStatus { idle, submitting, success, error }

class MaintenanceSubmitState {
  final SubmitStatus status;
  final String? errorMessage;
  final File? photo;
  final String? photoName;

  const MaintenanceSubmitState({
    this.status = SubmitStatus.idle,
    this.errorMessage,
    this.photo,
    this.photoName,
  });

  MaintenanceSubmitState copyWith({
    SubmitStatus? status,
    String? errorMessage,
    File? photo,
    String? photoName,
  }) =>
      MaintenanceSubmitState(
        status: status ?? this.status,
        errorMessage: errorMessage,
        photo: photo ?? this.photo,
        photoName: photoName ?? this.photoName,
      );
}

class MaintenanceSubmitNotifier
    extends StateNotifier<MaintenanceSubmitState> {
  final SupabaseService _service;
  final int tenantId;

  MaintenanceSubmitNotifier(this._service, this.tenantId)
      : super(const MaintenanceSubmitState());

  void setPhoto(File file, String name) {
    state = state.copyWith(photo: file, photoName: name, errorMessage: null);
  }

  void clearPhoto() {
    state = MaintenanceSubmitState(status: state.status);
  }

  Future<bool> submit({
    required String title,
    required String description,
  }) async {
    state = state.copyWith(status: SubmitStatus.submitting, errorMessage: null);
    try {
      await _service.submitMaintenanceRequest(
        tenantId: tenantId,
        title: title,
        description: description,
        photo: state.photo,
      );
      state = state.copyWith(status: SubmitStatus.success);
      return true;
    } on StorageException catch (e) {
      String message = e.message;
      final lower = e.message.toLowerCase();
      if (e.statusCode == '404' || lower.contains('bucket not found') || lower.contains('nosuchbucket')) {
        message = 'Storage bucket "maintenance-photos" not found in Supabase. Please create the bucket in your Supabase Dashboard.';
      } else if (e.statusCode == '403' || lower.contains('row-level security') || lower.contains('policy') || lower.contains('unauthorized')) {
        message = 'Storage permission denied. Please add an INSERT policy for authenticated users on "storage.objects" in Supabase.';
      }
      state = state.copyWith(status: SubmitStatus.error, errorMessage: message);
      return false;
    } on PostgrestException catch (e) {
      String message = 'Database error: ${e.message}';
      if (e.code == '42501' || e.message.toLowerCase().contains('policy')) {
        message = 'Database permission denied: Check RLS policies on the "maintenance_request" table.';
      }
      state = state.copyWith(status: SubmitStatus.error, errorMessage: message);
      return false;
    } on SocketException {
      state = state.copyWith(
        status: SubmitStatus.error,
        errorMessage: 'Network connection failed. Please check your internet connection.',
      );
      return false;
    } catch (e) {
      state = state.copyWith(
        status: SubmitStatus.error,
        errorMessage: 'Could not submit request: $e',
      );
      return false;
    }
  }

  void reset() => state = const MaintenanceSubmitState();
}

// Family parameterised by tenant int ID
final maintenanceSubmitProvider = StateNotifierProvider.autoDispose
    .family<MaintenanceSubmitNotifier, MaintenanceSubmitState, int>(
        (ref, tenantId) {
  return MaintenanceSubmitNotifier(
      ref.watch(supabaseServiceProvider), tenantId);
});
