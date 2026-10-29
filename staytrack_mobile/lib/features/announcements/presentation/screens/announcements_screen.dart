import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:staytrack_mobile/core/models/announcement.dart';
import 'package:staytrack_mobile/core/theme/app_theme.dart';
import 'package:staytrack_mobile/features/announcements/presentation/providers/announcements_provider.dart';

class AnnouncementsScreen extends ConsumerWidget {
  const AnnouncementsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final announcementsAsync = ref.watch(announcementsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Notices & Announcements'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(announcementsProvider),
        color: AppColors.primary,
        child: announcementsAsync.when(
          data: (announcements) {
            if (announcements.isEmpty) {
              return _EmptyAnnouncements();
            }
            return ListView.builder(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
              itemCount: announcements.length,
              itemBuilder: (context, i) => Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: _AnnouncementCard(
                  announcement: announcements[i],
                  isLatest: i == 0,
                )
                    .animate()
                    .fadeIn(delay: Duration(milliseconds: i * 60))
                    .slideY(begin: 0.05),
              ),
            );
          },
          loading: () => ListView.builder(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
            itemCount: 5,
            itemBuilder: (_, i) => Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: _AnnouncementCardSkeleton()
                  .animate(delay: Duration(milliseconds: i * 80))
                  .shimmer(duration: 1200.ms),
            ),
          ),
          error: (e, _) => Center(
            child: Padding(
              padding: const EdgeInsets.all(40),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.cloud_off_rounded,
                      size: 56, color: AppColors.textMuted),
                  const SizedBox(height: 16),
                  Text(
                    'Could not load announcements.\nPull down to try again.',
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
    );
  }
}

// ── Announcement card ─────────────────────────────────────────────────────────
class _AnnouncementCard extends StatefulWidget {
  final Announcement announcement;
  final bool isLatest;
  const _AnnouncementCard(
      {required this.announcement, required this.isLatest});

  @override
  State<_AnnouncementCard> createState() => _AnnouncementCardState();
}

class _AnnouncementCardState extends State<_AnnouncementCard> {
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    final a = widget.announcement;
    final dateStr = DateFormat('dd MMMM yyyy').format(a.createdAt);
    const maxLines = 3;

    return GestureDetector(
      onTap: () => setState(() => _expanded = !_expanded),
      child: Container(
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(20),
          border: widget.isLatest
              ? Border.all(color: AppColors.accent.withOpacity(0.3), width: 1.5)
              : null,
          boxShadow: [
            BoxShadow(
              color: AppColors.cardShadow.withOpacity(0.07),
              blurRadius: 16,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header row
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        colors: widget.isLatest
                            ? AppColors.accentGradient
                            : [AppColors.primary, AppColors.primaryDark],
                      ),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(
                      widget.isLatest
                          ? Icons.campaign_rounded
                          : Icons.notifications_rounded,
                      color: Colors.white,
                      size: 18,
                    ),
                  ),
                  const SizedBox(width: 12),
                  if (widget.isLatest)
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: AppColors.accent,
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: const Text(
                        'Latest',
                        style: TextStyle(
                            color: Colors.white,
                            fontSize: 10,
                            fontWeight: FontWeight.w700),
                      ),
                    ),
                  const Spacer(),
                  Text(dateStr,
                      style: Theme.of(context).textTheme.bodySmall),
                ],
              ),

              const SizedBox(height: 14),

              // Title
              Text(
                a.title,
                style: Theme.of(context).textTheme.titleMedium,
              ),

              const SizedBox(height: 8),

              // Message
              AnimatedCrossFade(
                duration: const Duration(milliseconds: 250),
                crossFadeState: _expanded
                    ? CrossFadeState.showSecond
                    : CrossFadeState.showFirst,
                firstChild: Text(
                  a.message,
                  style: Theme.of(context).textTheme.bodyMedium,
                  maxLines: maxLines,
                  overflow: TextOverflow.ellipsis,
                ),
                secondChild: Text(
                  a.message,
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ),

              const SizedBox(height: 10),

              // Read more / less
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  Text(
                    _expanded ? 'Show less' : 'Read more',
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: AppColors.primary,
                          fontWeight: FontWeight.w600,
                        ),
                  ),
                  const SizedBox(width: 4),
                  Icon(
                    _expanded
                        ? Icons.keyboard_arrow_up_rounded
                        : Icons.keyboard_arrow_down_rounded,
                    color: AppColors.primary,
                    size: 18,
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Skeleton ──────────────────────────────────────────────────────────────────
class _AnnouncementCardSkeleton extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      height: 130,
      decoration: BoxDecoration(
        color: AppColors.surfaceVariant,
        borderRadius: BorderRadius.circular(20),
      ),
    );
  }
}

// ── Empty ─────────────────────────────────────────────────────────────────────
class _EmptyAnnouncements extends StatelessWidget {
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
              child: const Icon(Icons.campaign_outlined,
                  color: AppColors.textMuted, size: 36),
            ),
            const SizedBox(height: 20),
            Text(
              'No announcements yet.\nCheck back soon for updates from management.',
              textAlign: TextAlign.center,
              style: Theme.of(context)
                  .textTheme
                  .bodyLarge
                  ?.copyWith(color: AppColors.textSecondary),
            ),
          ],
        ),
      ),
    );
  }
}
