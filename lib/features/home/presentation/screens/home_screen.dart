import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:stay_track/core/constants/app_constants.dart';
import 'package:stay_track/core/models/tenant.dart';
import 'package:stay_track/core/models/user_profile.dart';
import 'package:stay_track/core/theme/app_theme.dart';
import 'package:stay_track/features/auth/presentation/providers/auth_provider.dart';
import 'package:stay_track/features/home/presentation/providers/home_provider.dart';
import 'package:stay_track/core/services/supabase_service.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profileAsync = ref.watch(profileProvider);
    final tenantAsync = ref.watch(tenantProvider);
    final announcementAsync = ref.watch(latestAnnouncementProvider);

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(profileProvider);
          ref.invalidate(tenantProvider);
          ref.invalidate(latestAnnouncementProvider);
        },
        color: AppColors.primary,
        child: CustomScrollView(
          slivers: [
            // ── Hero header ───────────────────────────────────────────────────
            SliverToBoxAdapter(
              child: profileAsync.when(
                data: (profile) => tenantAsync.when(
                  data: (tenant) => _HeroHeader(profile: profile, tenant: tenant),
                  loading: () => _HeroHeader(profile: profile, tenant: null),
                  error: (_, __) => _HeroHeader(profile: profile, tenant: null),
                ),
                loading: () => const _HeroHeaderSkeleton(),
                error: (_, __) => _HeroHeader(profile: null, tenant: null),
              ),
            ),

            // ── Quick actions ─────────────────────────────────────────────────
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 24, 20, 0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Quick Actions',
                        style: Theme.of(context).textTheme.titleMedium),
                    const SizedBox(height: 16),
                    _QuickActionsRow(),
                  ],
                ),
              ).animate().fadeIn(delay: 200.ms).slideY(begin: 0.1),
            ),

            // ── Rent status card ──────────────────────────────────────────────
            SliverToBoxAdapter(
              child: tenantAsync.when(
                data: (tenant) => Padding(
                  padding: const EdgeInsets.fromLTRB(20, 24, 20, 0),
                  child: _RentStatusCard(tenant: tenant),
                ).animate().fadeIn(delay: 300.ms).slideY(begin: 0.1),
                loading: () => const _SkeletonCard(height: 140),
                error: (_, __) => const SizedBox.shrink(),
              ),
            ),

            // ── Latest announcement ───────────────────────────────────────────
            SliverToBoxAdapter(
              child: announcementAsync.when(
                data: (ann) => ann != null
                    ? Padding(
                        padding: const EdgeInsets.fromLTRB(20, 24, 20, 0),
                        child: _AnnouncementCard(
                          title: ann.title,
                          message: ann.message,
                          date: ann.createdAt,
                        ),
                      ).animate().fadeIn(delay: 400.ms).slideY(begin: 0.1)
                    : const SizedBox.shrink(),
                loading: () => const _SkeletonCard(height: 120),
                error: (_, __) => const SizedBox.shrink(),
              ),
            ),

            // bottom padding
            const SliverToBoxAdapter(child: SizedBox(height: 32)),
          ],
        ),
      ),
    );
  }
}

// ── Hero header ───────────────────────────────────────────────────────────────
class _HeroHeader extends ConsumerWidget {
  final UserProfile? profile;
  final Tenant? tenant;
  const _HeroHeader({this.profile, this.tenant});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final firstName = profile?.fullName.split(' ').first ?? 'Student';
    final room = tenant?.room?.roomNumber ?? (tenant?.roomId?.toString() ?? '—');
    final ref2 = tenant?.paymentReference ?? '—';

    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          colors: AppColors.heroGradient,
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      padding: const EdgeInsets.fromLTRB(24, 60, 24, 32),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Greeting + sign out
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Good ${_timeOfDay()}, 👋',
                      style: const TextStyle(
                        color: Colors.white70,
                        fontSize: 14,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    Text(
                      firstName,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 26,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ],
                ),
              ),
              IconButton(
                onPressed: () async {
                  await ref.read(supabaseServiceProvider).signOut();
                },
                icon: const Icon(Icons.logout_rounded, color: Colors.white70),
                tooltip: 'Sign out',
              ),
            ],
          ),

          const SizedBox(height: 24),

          // Room + Ref chips
          Row(
            children: [
              _InfoChip(
                icon: Icons.meeting_room_rounded,
                label: 'Room',
                value: room,
              ),
              const SizedBox(width: 12),
              _InfoChip(
                icon: Icons.tag_rounded,
                label: 'Payment Ref',
                value: ref2,
              ),
            ],
          ),
        ],
      ),
    ).animate().fadeIn(duration: 600.ms);
  }

  String _timeOfDay() {
    final h = DateTime.now().hour;
    if (h < 12) return 'morning';
    if (h < 17) return 'afternoon';
    return 'evening';
  }
}

class _InfoChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  const _InfoChip(
      {required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: Colors.white.withOpacity(0.15),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.white.withOpacity(0.2)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, color: Colors.white70, size: 16),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label,
                  style: const TextStyle(color: Colors.white54, fontSize: 10)),
              Text(value,
                  style: const TextStyle(
                      color: Colors.white,
                      fontSize: 13,
                      fontWeight: FontWeight.w700)),
            ],
          ),
        ],
      ),
    );
  }
}

// ── Quick Actions ─────────────────────────────────────────────────────────────
class _QuickActionsRow extends StatelessWidget {
  final _actions = const [
    _QuickAction(
      icon: Icons.upload_file_rounded,
      label: 'Upload\nProof',
      route: '/payments/upload',
      color: AppColors.primary,
    ),
    _QuickAction(
      icon: Icons.receipt_long_rounded,
      label: 'My\nPayments',
      route: '/payments',
      color: AppColors.accent,
    ),
    _QuickAction(
      icon: Icons.build_rounded,
      label: 'Log\nIssue',
      route: '/maintenance/submit',
      color: AppColors.warning,
    ),
    _QuickAction(
      icon: Icons.campaign_rounded,
      label: 'Notices',
      route: '/announcements',
      color: Color(0xFF7C3AED),
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: _actions
          .map((a) => _QuickActionButton(action: a))
          .toList(),
    );
  }
}

class _QuickAction {
  final IconData icon;
  final String label;
  final String route;
  final Color color;
  const _QuickAction(
      {required this.icon,
      required this.label,
      required this.route,
      required this.color});
}

class _QuickActionButton extends StatelessWidget {
  final _QuickAction action;
  const _QuickActionButton({required this.action});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => context.push(action.route),
      child: Column(
        children: [
          Container(
            width: 64,
            height: 64,
            decoration: BoxDecoration(
              color: action.color.withOpacity(0.1),
              borderRadius: BorderRadius.circular(18),
              border: Border.all(color: action.color.withOpacity(0.2)),
            ),
            child: Icon(action.icon, color: action.color, size: 28),
          ),
          const SizedBox(height: 8),
          Text(
            action.label,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  fontWeight: FontWeight.w600,
                  color: AppColors.onBackground,
                  height: 1.3,
                ),
          ),
        ],
      ),
    );
  }
}

// ── Rent status card ──────────────────────────────────────────────────────────
class _RentStatusCard extends StatelessWidget {
  final Tenant? tenant;
  const _RentStatusCard({this.tenant});

  @override
  Widget build(BuildContext context) {
    final amount = tenant?.rentalAmount ?? 0.0;
    final leaseEnd = tenant?.leaseEnd;
    final dueDate = leaseEnd != null
        ? DateFormat('dd MMM yyyy').format(leaseEnd)
        : 'Not set';
    final currency = NumberFormat.simpleCurrency(locale: 'en_ZA');

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: AppColors.cardShadow.withOpacity(0.08),
            blurRadius: 24,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: AppColors.success.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(Icons.home_work_rounded,
                    color: AppColors.success, size: 20),
              ),
              const SizedBox(width: 12),
              Text('Rent Status',
                  style: Theme.of(context).textTheme.titleMedium),
            ],
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              Expanded(
                child: _RentStat(
                  label: 'Monthly Rent',
                  value: currency.format(amount),
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: _RentStat(
                  label: 'Next Due Date',
                  value: dueDate,
                  color: AppColors.warning,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _RentStat extends StatelessWidget {
  final String label;
  final String value;
  final Color color;
  const _RentStat(
      {required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: Theme.of(context)
                .textTheme
                .bodySmall
                ?.copyWith(color: AppColors.textMuted)),
        const SizedBox(height: 4),
        Text(value,
            style: Theme.of(context)
                .textTheme
                .titleMedium
                ?.copyWith(color: color)),
      ],
    );
  }
}

// ── Latest announcement card ──────────────────────────────────────────────────
class _AnnouncementCard extends StatelessWidget {
  final String title;
  final String message;
  final DateTime date;
  const _AnnouncementCard(
      {required this.title, required this.message, required this.date});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            AppColors.accent.withOpacity(0.08),
            AppColors.primary.withOpacity(0.04),
          ],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.accent.withOpacity(0.2)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.accent,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: const Text(
                  '📢 Latest Notice',
                  style: TextStyle(
                      color: Colors.white,
                      fontSize: 11,
                      fontWeight: FontWeight.w700),
                ),
              ),
              const Spacer(),
              Text(
                DateFormat('dd MMM').format(date),
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(title,
              style: Theme.of(context).textTheme.titleMedium,
              maxLines: 2,
              overflow: TextOverflow.ellipsis),
          const SizedBox(height: 6),
          Text(message,
              style: Theme.of(context).textTheme.bodyMedium,
              maxLines: 3,
              overflow: TextOverflow.ellipsis),
        ],
      ),
    );
  }
}

// ── Skeletons ─────────────────────────────────────────────────────────────────
class _HeroHeaderSkeleton extends StatelessWidget {
  const _HeroHeaderSkeleton();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 200,
      decoration: const BoxDecoration(
        gradient: LinearGradient(colors: AppColors.heroGradient),
      ),
    );
  }
}

class _SkeletonCard extends StatelessWidget {
  final double height;
  const _SkeletonCard({required this.height});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 24, 20, 0),
      child: Container(
        height: height,
        decoration: BoxDecoration(
          color: AppColors.surfaceVariant,
          borderRadius: BorderRadius.circular(20),
        ),
      ),
    ).animate(onPlay: (c) => c.repeat(reverse: true))
        .shimmer(duration: 1200.ms, color: Colors.white.withOpacity(0.6));
  }
}
