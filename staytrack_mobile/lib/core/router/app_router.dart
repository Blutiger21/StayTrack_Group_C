import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:staytrack_mobile/features/auth/presentation/providers/auth_provider.dart';
import 'package:staytrack_mobile/features/auth/presentation/screens/sign_in_screen.dart';
import 'package:staytrack_mobile/features/auth/presentation/screens/otp_screen.dart';
import 'package:staytrack_mobile/features/home/presentation/screens/home_screen.dart';
import 'package:staytrack_mobile/features/home/presentation/screens/main_shell.dart';
import 'package:staytrack_mobile/features/payments/presentation/screens/payments_screen.dart';
import 'package:staytrack_mobile/features/payments/presentation/screens/upload_proof_screen.dart';
import 'package:staytrack_mobile/features/maintenance/presentation/screens/maintenance_screen.dart';
import 'package:staytrack_mobile/features/maintenance/presentation/screens/submit_request_screen.dart';
import 'package:staytrack_mobile/features/announcements/presentation/screens/announcements_screen.dart';

final _rootNavigatorKey = GlobalKey<NavigatorState>();
final _shellNavigatorKey = GlobalKey<NavigatorState>();

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authStateProvider);

  return GoRouter(
    navigatorKey: _rootNavigatorKey,
    debugLogDiagnostics: false,
    initialLocation: '/sign-in',
    redirect: (context, state) {
      final isAuthenticated = authState.valueOrNull?.session != null;
      final isOnAuthPage = state.matchedLocation.startsWith('/sign-in') ||
          state.matchedLocation.startsWith('/otp');

      if (!isAuthenticated && !isOnAuthPage) return '/sign-in';
      if (isAuthenticated && isOnAuthPage) return '/home';
      return null;
    },
    routes: [
      // Auth flow (no shell)
      GoRoute(
        path: '/sign-in',
        builder: (_, __) => const SignInScreen(),
      ),
      GoRoute(
        path: '/otp',
        builder: (_, state) {
          final email = state.extra as String? ?? '';
          return OtpScreen(email: email);
        },
      ),

      // Full-screen modal flows (outside bottom nav shell)
      GoRoute(
        path: '/payments/upload',
        builder: (_, __) => const UploadProofScreen(),
      ),
      GoRoute(
        path: '/maintenance/submit',
        builder: (_, __) => const SubmitRequestScreen(),
      ),

      // Main shell with bottom nav
      ShellRoute(
        navigatorKey: _shellNavigatorKey,
        builder: (_, __, child) => MainShell(child: child),
        routes: [
          GoRoute(path: '/home', builder: (_, __) => const HomeScreen()),
          GoRoute(path: '/payments', builder: (_, __) => const PaymentsScreen()),
          GoRoute(path: '/maintenance', builder: (_, __) => const MaintenanceScreen()),
          GoRoute(path: '/announcements', builder: (_, __) => const AnnouncementsScreen()),
        ],
      ),
    ],
  );
});
