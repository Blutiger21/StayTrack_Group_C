import 'dart:io';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import 'package:stay_track/core/models/payment.dart';
import 'package:stay_track/core/services/supabase_service.dart';
import 'package:stay_track/features/home/presentation/providers/home_provider.dart';

// ── Payments list ─────────────────────────────────────────────────────────────
final paymentsProvider = FutureProvider<List<Payment>>((ref) async {
  final tenant = await ref.watch(tenantProvider.future);
  if (tenant == null) return [];
  return ref.watch(supabaseServiceProvider).getPaymentsByTenant(tenant.tenantId);
});

// ── Upload state ──────────────────────────────────────────────────────────────
enum UploadStatus { idle, uploading, success, error }

class UploadState {
  final UploadStatus status;
  final String? errorMessage;
  final File? selectedFile;
  final String? selectedFileName;

  const UploadState({
    this.status = UploadStatus.idle,
    this.errorMessage,
    this.selectedFile,
    this.selectedFileName,
  });

  UploadState copyWith({
    UploadStatus? status,
    String? errorMessage,
    File? selectedFile,
    String? selectedFileName,
  }) =>
      UploadState(
        status: status ?? this.status,
        errorMessage: errorMessage,
        selectedFile: selectedFile ?? this.selectedFile,
        selectedFileName: selectedFileName ?? this.selectedFileName,
      );
}

class UploadNotifier extends StateNotifier<UploadState> {
  final SupabaseService _service;
  final int tenantId;

  UploadNotifier(this._service, this.tenantId) : super(const UploadState());

  void selectFile(File file, String name) {
    state = state.copyWith(
      selectedFile: file,
      selectedFileName: name,
      status: UploadStatus.idle,
      errorMessage: null,
    );
  }

  void clearFile() {
    state = const UploadState();
  }

  Future<bool> upload({
    required double amount,
    required String periodMonth,
    required String reference,
  }) async {
    if (state.selectedFile == null) {
      state = state.copyWith(
        status: UploadStatus.error,
        errorMessage: 'Please select a file first.',
      );
      return false;
    }

    state = state.copyWith(status: UploadStatus.uploading, errorMessage: null);
    try {
      await _service.submitPaymentProof(
        tenantId: tenantId,
        amount: amount,
        periodMonth: periodMonth,
        reference: reference,
        file: state.selectedFile!,
      );
      state = state.copyWith(status: UploadStatus.success);
      return true;
    } on StorageException catch (e) {
      String message = e.message;
      final lower = e.message.toLowerCase();
      if (e.statusCode == '404' || lower.contains('bucket not found') || lower.contains('nosuchbucket')) {
        message = 'Storage bucket "proof-of-payment" not found in Supabase. Please create the bucket in your Supabase Dashboard.';
      } else if (e.statusCode == '403' || lower.contains('row-level security') || lower.contains('policy') || lower.contains('unauthorized')) {
        message = 'Storage permission denied. Please add an INSERT policy for authenticated users on "storage.objects" in Supabase.';
      }
      state = state.copyWith(status: UploadStatus.error, errorMessage: message);
      return false;
    } on PostgrestException catch (e) {
      String message = 'Database error: ${e.message}';
      if (e.code == '42501' || e.message.toLowerCase().contains('policy')) {
        message = 'Database permission denied: Check RLS policies on the "payment" table.';
      }
      state = state.copyWith(status: UploadStatus.error, errorMessage: message);
      return false;
    } on SocketException {
      state = state.copyWith(
        status: UploadStatus.error,
        errorMessage: 'Network connection failed. Please check your internet connection.',
      );
      return false;
    } catch (e) {
      state = state.copyWith(
        status: UploadStatus.error,
        errorMessage: 'Upload failed: $e',
      );
      return false;
    }
  }
}

// Family parameterised by tenant int ID
final uploadNotifierFamilyProvider = StateNotifierProvider.autoDispose
    .family<UploadNotifier, UploadState, int>((ref, tenantId) {
  return UploadNotifier(ref.watch(supabaseServiceProvider), tenantId);
});
