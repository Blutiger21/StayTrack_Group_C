import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import 'package:stay_track/core/services/supabase_service.dart';

// ── Auth state stream ─────────────────────────────────────────────────────────
final authStateProvider = StreamProvider<AuthState>((ref) {
  final service = ref.watch(supabaseServiceProvider);
  return service.authStateChanges;
});

// ── Current user ──────────────────────────────────────────────────────────────
final currentUserProvider = Provider<User?>((ref) {
  return Supabase.instance.client.auth.currentUser;
});

// ── OTP notifier ──────────────────────────────────────────────────────────────
enum OtpStep { idle, sending, sent, verifying, verified, error }

class OtpState {
  final OtpStep step;
  final String? errorMessage;

  const OtpState({this.step = OtpStep.idle, this.errorMessage});

  OtpState copyWith({OtpStep? step, String? errorMessage}) => OtpState(
    step: step ?? this.step,
    errorMessage: errorMessage,
  );
}

class OtpNotifier extends StateNotifier<OtpState> {
  final SupabaseService _service;
  OtpNotifier(this._service) : super(const OtpState());

  Future<void> sendOtp(String email) async {
    state = state.copyWith(step: OtpStep.sending, errorMessage: null);
    try {
      await _service.sendOtp(email.trim().toLowerCase());
      state = state.copyWith(step: OtpStep.sent);
    } on AuthException catch (e) {
      state = state.copyWith(
        step: OtpStep.error,
        errorMessage: _friendlyAuthError(e.message),
      );
    } catch (e) {
      state = state.copyWith(
        step: OtpStep.error,
        errorMessage: 'Something went wrong. Please check your connection and try again.',
      );
    }
  }

  Future<bool> verifyOtp(String email, String otp) async {
    state = state.copyWith(step: OtpStep.verifying, errorMessage: null);
    try {
      final response = await _service.verifyOtp(email.trim().toLowerCase(), otp.trim());
      if (response.session != null) {
        state = state.copyWith(step: OtpStep.verified);
        return true;
      }
      state = state.copyWith(
        step: OtpStep.error,
        errorMessage: 'Verification failed. Please try again.',
      );
      return false;
    } on AuthException catch (e) {
      state = state.copyWith(
        step: OtpStep.error,
        errorMessage: _friendlyAuthError(e.message),
      );
      return false;
    } catch (e) {
      state = state.copyWith(
        step: OtpStep.error,
        errorMessage: 'Could not verify your code. Please try again.',
      );
      return false;
    }
  }

  void reset() => state = const OtpState();

  String _friendlyAuthError(String raw) {
    final lower = raw.toLowerCase();
    if (lower.contains('invalid') && lower.contains('token')) {
      return 'That code is incorrect. Please double-check and try again.';
    }
    if (lower.contains('expired')) {
      return 'Your code has expired. Please request a new one.';
    }
    if (lower.contains('rate limit')) {
      return 'Too many attempts. Please wait a moment before trying again.';
    }
    return raw;
  }
}

final otpNotifierProvider =
StateNotifierProvider<OtpNotifier, OtpState>((ref) {
  return OtpNotifier(ref.watch(supabaseServiceProvider));
});
