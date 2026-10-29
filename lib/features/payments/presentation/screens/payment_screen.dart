import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:stay_track/core/constants/app_constants.dart';
import 'package:stay_track/core/models/payment.dart';
import 'package:stay_track/core/theme/app_theme.dart';
import 'package:stay_track/features/home/presentation/providers/home_provider.dart';
import 'package:stay_track/features/payments/presentation/providers/payments_provider.dart';

class PaymentsScreen extends ConsumerWidget {
  const PaymentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tenantAsync = ref.watch(tenantProvider);
    final paymentsAsync = ref.watch(paymentsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Payments'),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: FilledButton.tonal(
              onPressed: () => context.push('/payments/upload'),
              style: FilledButton.styleFrom(
                backgroundColor: AppColors.primary.withOpacity(0.1),
                foregroundColor: AppColors.primary,
              ),
              child: const Text('Upload Proof'),
            ),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(paymentsProvider),
        color: AppColors.primary,
        child: CustomScrollView(
          slivers: [
            // ── Outstanding balance card ──────────────────────────────────────
            SliverToBoxAdapter(
              child: tenantAsync.when(
                data: (tenant) => Padding(
                  padding: const EdgeInsets.fromLTRB(20, 20, 20, 0),
                  child: _BalanceSummaryCard(
                    rentalAmount: tenant?.rentalAmount ?? 0,
                    payments: paymentsAsync.valueOrNull ?? [],
                  ),
                ).animate().fadeIn().slideY(begin: 0.1),
                loading: () => const _SkeletonCard(height: 130),
                error: (_, __) => const SizedBox.shrink(),
              ),
            ),

            // ── Section header ────────────────────────────────────────────────
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 28, 20, 12),
                child: Text(
                  'Payment History',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
            ),

            // ── Payment list ──────────────────────────────────────────────────
            paymentsAsync.when(
              data: (payments) {
                if (payments.isEmpty) {
                  return SliverFillRemaining(
                    child: _EmptyState(
                      icon: Icons.receipt_long_outlined,
                      message: 'No payment records found yet.\nSubmit your first proof of payment to get started.',
                      actionLabel: 'Upload Proof',
                      onAction: () => context.push('/payments/upload'),
                    ),
                  );
                }
                return SliverList.builder(
                  itemCount: payments.length,
                  itemBuilder: (context, i) => Padding(
                    padding: const EdgeInsets.fromLTRB(20, 0, 20, 12),
                    child: _PaymentTile(
                      payment: payments[i],
                    ).animate().fadeIn(delay: Duration(milliseconds: i * 60)).slideX(begin: -0.05),
                  ),
                );
              },
              loading: () => SliverList.builder(
                itemCount: 5,
                itemBuilder: (_, i) => const Padding(
                  padding: EdgeInsets.fromLTRB(20, 0, 20, 12),
                  child: _SkeletonCard(height: 80),
                ),
              ),
              error: (e, _) => const SliverFillRemaining(
                child: _EmptyState(
                  icon: Icons.error_outline_rounded,
                  message: 'Could not load your payment history.\nPlease try again.',
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

// ── Balance summary card ───────────────────────────────────────────────────────
class _BalanceSummaryCard extends StatelessWidget {
  final double rentalAmount;
  final List<Payment> payments;

  const _BalanceSummaryCard(
      {required this.rentalAmount, required this.payments});

  @override
  Widget build(BuildContext context) {
    final unpaid = payments.where((p) => !p.isVerified).length;
    final outstanding = unpaid * rentalAmount;
    final currency = NumberFormat.simpleCurrency(locale: 'en_ZA');

    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: AppColors.heroGradient,
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(22),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withOpacity(0.3),
            blurRadius: 20,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Outstanding Balance',
              style: TextStyle(color: Colors.white70, fontSize: 13)),
          const SizedBox(height: 4),
          Text(
            currency.format(outstanding),
            style: const TextStyle(
                color: Colors.white,
                fontSize: 32,
                fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              _PillStat(
                  label: 'Monthly',
                  value: currency.format(rentalAmount),
                  color: Colors.white),
              const SizedBox(width: 12),
              _PillStat(
                  label: 'Pending',
                  value: '$unpaid payment${unpaid == 1 ? '' : 's'}',
                  color: Colors.white),
            ],
          ),
        ],
      ),
    );
  }
}

class _PillStat extends StatelessWidget {
  final String label;
  final String value;
  final Color color;
  const _PillStat(
      {required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.white.withOpacity(0.15),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: const TextStyle(color: Colors.white60, fontSize: 10)),
          Text(value,
              style: TextStyle(
                  color: color, fontSize: 13, fontWeight: FontWeight.w700)),
        ],
      ),
    );
  }
}

// ── Payment tile ───────────────────────────────────────────────────────────────
class _PaymentTile extends StatelessWidget {
  final Payment payment;
  const _PaymentTile({required this.payment});

  @override
  Widget build(BuildContext context) {
    final currency = NumberFormat.simpleCurrency(locale: 'en_ZA');
    final date = payment.paymentDate != null
        ? DateFormat('dd MMM yyyy').format(payment.paymentDate!)
        : '—';

    Color statusColor;
    IconData statusIcon;
    switch (payment.status) {
      case AppConstants.paymentVerified:
        statusColor = AppColors.success;
        statusIcon = Icons.check_circle_rounded;
        break;
      case AppConstants.paymentRejected:
        statusColor = AppColors.error;
        statusIcon = Icons.cancel_rounded;
        break;
      default:
        statusColor = AppColors.warning;
        statusIcon = Icons.schedule_rounded;
    }

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: AppColors.cardShadow.withOpacity(0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Row(
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
                Text(
                  payment.periodLabel.isNotEmpty ? payment.periodLabel : 'Payment',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontSize: 15,
                      ),
                ),
                const SizedBox(height: 2),
                Text(date, style: Theme.of(context).textTheme.bodySmall),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                currency.format(payment.amount),
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(color: AppColors.primary),
              ),
              const SizedBox(height: 4),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: statusColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  payment.status,
                  style: TextStyle(
                    color: statusColor,
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// ── Empty state ───────────────────────────────────────────────────────────────
class _EmptyState extends StatelessWidget {
  final IconData icon;
  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;

  const _EmptyState({
    required this.icon,
    required this.message,
    this.actionLabel,
    this.onAction,
  });

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
              child: Icon(icon, color: AppColors.textMuted, size: 36),
            ),
            const SizedBox(height: 20),
            Text(
              message,
              textAlign: TextAlign.center,
              style: Theme.of(context)
                  .textTheme
                  .bodyLarge
                  ?.copyWith(color: AppColors.textSecondary),
            ),
            if (onAction != null) ...[
              const SizedBox(height: 24),
              OutlinedButton(
                onPressed: onAction,
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size(180, 48),
                ),
                child: Text(actionLabel!),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _SkeletonCard extends StatelessWidget {
  final double height;
  const _SkeletonCard({required this.height});

  @override
  Widget build(BuildContext context) {
    return Container(
      height: height,
      decoration: BoxDecoration(
        color: AppColors.surfaceVariant,
        borderRadius: BorderRadius.circular(16),
      ),
    ).animate(onPlay: (c) => c.repeat(reverse: true))
        .shimmer(duration: 1200.ms, color: Colors.white.withOpacity(0.6));
  }
}
