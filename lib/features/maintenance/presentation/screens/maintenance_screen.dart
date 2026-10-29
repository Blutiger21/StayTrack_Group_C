import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:stay_track/core/constants/app_constants.dart';
import 'package:stay_track/core/models/maintenance_request.dart';
import 'package:stay_track/core/theme/app_theme.dart';
import 'package:stay_track/features/maintenance/presentation/providers/maintenance_provider.dart';

class MaintenanceScreen extends ConsumerWidget {
  const MaintenanceScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final requestsAsync = ref.watch(maintenanceRequestsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Maintenance Feed'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: FilledButton.tonal(
              onPressed: () => context.push('/maintenance/submit'),
              style: FilledButton.styleFrom(
                backgroundColor: AppColors.warning.withOpacity(0.1),
                foregroundColor: AppColors.warning,
              ),
              child: const Text('Log Issue'),
            ),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(maintenanceRequestsProvider),
        color: AppColors.primary,
        child: CustomScrollView(
          slivers: [
            // ── Info banner ───────────────────────────────────────────────────
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 16, 20, 4),
                child: Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: AppColors.warning.withOpacity(0.07),
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(
                        color: AppColors.warning.withOpacity(0.2)),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.people_alt_outlined,
                          color: AppColors.warning, size: 18),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(
                          'This is a shared feed. Check here before logging a new issue to avoid duplicate reports.',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: AppColors.warning),
                        ),
                      ),
                    ],
                  ),
                ),
              ).animate().fadeIn(),
            ),

            // ── Requests list ─────────────────────────────────────────────────
            requestsAsync.when(
              data: (requests) {
                if (requests.isEmpty) {
                  return SliverFillRemaining(
                    child: _EmptyFeed(
                      onLog: () => context.push('/maintenance/submit'),
                    ),
                  );
                }
                return SliverList.builder(
                  itemCount: requests.length,
                  itemBuilder: (context, i) => Padding(
                    padding: const EdgeInsets.fromLTRB(20, 12, 20, 0),
                    child: _RequestCard(request: requests[i])
                        .animate()
                        .fadeIn(delay: Duration(milliseconds: i * 50))
                        .slideX(begin: -0.05),
                  ),
                );
              },
              loading: () => SliverList.builder(
                itemCount: 5,
                itemBuilder: (_, i) => Padding(
                  padding: const EdgeInsets.fromLTRB(20, 12, 20, 0),
                  child: _SkeletonCard()
                      .animate(delay: Duration(milliseconds: i * 80))
                      .shimmer(duration: 1200.ms),
                ),
              ),
              error: (_, __) => SliverFillRemaining(
                child: Center(
                  child: Padding(
                    padding: const EdgeInsets.all(40),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.cloud_off_rounded,
                            size: 56, color: AppColors.textMuted),
                        const SizedBox(height: 16),
                        Text(
                          'Could not load maintenance requests.\nPlease check your connection and pull down to retry.',
                          textAlign: TextAlign.center,
                          style: Theme.of(context)
                              .textTheme
                              .bodyLarge
                              ?.copyWith(color: AppColors.textSecondary),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),

            const SliverToBoxAdapter(child: SizedBox(height: 32)),
          ],
        ),
      ),
    );
  }
}

// ── Request card ──────────────────────────────────────────────────────────────
class _RequestCard extends StatelessWidget {
  final MaintenanceRequest request;
  const _RequestCard({required this.request});

  @override
  Widget build(BuildContext context) {
    Color statusColor;
    IconData statusIcon;
    switch (request.status) {
      case AppConstants.statusResolved:
        statusColor = AppColors.success;
        statusIcon = Icons.check_circle_rounded;
        break;
      case AppConstants.statusInProgress:
        statusColor = AppColors.primary;
        statusIcon = Icons.pending_rounded;
        break;
      default:
        statusColor = AppColors.warning;
        statusIcon = Icons.schedule_rounded;
    }


    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(18),
        boxShadow: [
          BoxShadow(
            color: AppColors.cardShadow.withOpacity(0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          // Top row
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 12),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    color: statusColor.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(statusIcon, color: statusColor, size: 22),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(request.title,
                          style: Theme.of(context).textTheme.titleMedium,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis),
                      const SizedBox(height: 4),
                      Text(request.description,
                          style: Theme.of(context).textTheme.bodyMedium,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis),
                    ],
                  ),
                ),
              ],
            ),
          ),

          // Divider
          const Divider(height: 1),

          // Footer
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                const Spacer(),
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: statusColor.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    request.status,
                    style: TextStyle(
                        color: statusColor,
                        fontSize: 10,
                        fontWeight: FontWeight.w700),
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  DateFormat('dd MMM').format(request.dateSubmitted),
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _EmptyFeed extends StatelessWidget {
  final VoidCallback onLog;
  const _EmptyFeed({required this.onLog});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(40),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 80,
              height: 80,
              decoration: BoxDecoration(
                color: AppColors.surfaceVariant,
                borderRadius: BorderRadius.circular(24),
              ),
              child:
                  const Icon(Icons.build_outlined, color: AppColors.textMuted, size: 36),
            ),
            const SizedBox(height: 20),
            Text(
              'No maintenance issues reported yet.\nEverything seems to be working! 🎉',
              textAlign: TextAlign.center,
              style: Theme.of(context)
                  .textTheme
                  .bodyLarge
                  ?.copyWith(color: AppColors.textSecondary),
            ),
            const SizedBox(height: 24),
            OutlinedButton(
              onPressed: onLog,
              style: OutlinedButton.styleFrom(minimumSize: const Size(180, 48)),
              child: const Text('Log an Issue'),
            ),
          ],
        ),
      ),
    );
  }
}

class _SkeletonCard extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      height: 110,
      decoration: BoxDecoration(
        color: AppColors.surfaceVariant,
        borderRadius: BorderRadius.circular(18),
      ),
    );
  }
}
