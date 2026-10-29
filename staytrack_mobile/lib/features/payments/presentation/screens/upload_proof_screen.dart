import 'dart:io';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:staytrack_mobile/core/theme/app_theme.dart';
import 'package:staytrack_mobile/features/home/presentation/providers/home_provider.dart';
import 'package:staytrack_mobile/features/payments/presentation/providers/payments_provider.dart';

class UploadProofScreen extends ConsumerStatefulWidget {
  const UploadProofScreen({super.key});

  @override
  ConsumerState<UploadProofScreen> createState() => _UploadProofScreenState();
}

class _UploadProofScreenState extends ConsumerState<UploadProofScreen> {
  final _formKey = GlobalKey<FormState>();
  final _amountCtrl = TextEditingController();
  String _selectedMonth =
      DateFormat('yyyy-MM').format(DateTime.now()); // e.g. "2024-03"

  // Month options: current + 2 past
  List<String> get _monthOptions {
    final now = DateTime.now();
    return List.generate(3, (i) {
      final d = DateTime(now.year, now.month - i, 1);
      return DateFormat('yyyy-MM').format(d);
    });
  }

  @override
  void dispose() {
    _amountCtrl.dispose();
    super.dispose();
  }

  Future<void> _pickFile(UploadNotifier notifier) async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png'],
      allowMultiple: false,
    );
    if (result != null && result.files.single.path != null) {
      notifier.selectFile(
        File(result.files.single.path!),
        result.files.single.name,
      );
    }
  }

  Future<void> _submit(UploadNotifier notifier, int tenantId, String reference) async {
    if (!_formKey.currentState!.validate()) return;

    final amount = double.tryParse(_amountCtrl.text.replaceAll(',', '.')) ?? 0;
    // period_month must be a full ISO date string e.g. "2025-03-01"
    final periodMonthDate = '$_selectedMonth-01';
    final success = await notifier.upload(
      amount: amount,
      periodMonth: periodMonthDate,
      reference: reference,
    );

    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('\u2705 Proof of payment submitted successfully!'),
          backgroundColor: AppColors.success,
        ),
      );
      ref.invalidate(paymentsProvider);
      context.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final tenantAsync = ref.watch(tenantProvider);

    return tenantAsync.when(
      data: (tenant) {
        final tenantId = tenant?.tenantId ?? 0;
        final reference = tenant?.paymentReference ?? '';
        final uploadState =
            ref.watch(uploadNotifierFamilyProvider(tenantId));
        final notifier =
            ref.read(uploadNotifierFamilyProvider(tenantId).notifier);

        return Scaffold(
          appBar: AppBar(title: const Text('Upload Proof of Payment')),
          body: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Header
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.primary.withOpacity(0.06),
                      borderRadius: BorderRadius.circular(16),
                      border:
                          Border.all(color: AppColors.primary.withOpacity(0.15)),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.info_outline_rounded,
                            color: AppColors.primary),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            'Please upload your bank-stamped proof of payment (PDF or image). Our team will verify it within 1-2 business days.',
                            style: Theme.of(context)
                                .textTheme
                                .bodyMedium
                                ?.copyWith(color: AppColors.primary),
                          ),
                        ),
                      ],
                    ),
                  ).animate().fadeIn(),

                  const SizedBox(height: 28),

                  // Amount field
                  Text('Amount Paid',
                      style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 10),
                  TextFormField(
                    controller: _amountCtrl,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(
                      prefixText: 'R ',
                      hintText: '0.00',
                    ),
                    validator: (v) {
                      if (v == null || v.trim().isEmpty) {
                        return 'Please enter the amount you paid.';
                      }
                      final parsed =
                          double.tryParse(v.replaceAll(',', '.'));
                      if (parsed == null || parsed <= 0) {
                        return 'Please enter a valid amount greater than zero.';
                      }
                      return null;
                    },
                  ).animate().fadeIn(delay: 100.ms),

                  const SizedBox(height: 24),

                  // Month selector
                  Text('Payment For Month',
                      style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 10),
                  DropdownButtonFormField<String>(
                    value: _selectedMonth,
                    decoration: const InputDecoration(
                      prefixIcon: Icon(Icons.calendar_today_rounded),
                    ),
                    items: _monthOptions.map((m) {
                      final label = DateFormat('MMMM yyyy')
                          .format(DateFormat('yyyy-MM').parse(m));
                      return DropdownMenuItem(value: m, child: Text(label));
                    }).toList(),
                    onChanged: (v) => setState(() => _selectedMonth = v!),
                  ).animate().fadeIn(delay: 150.ms),

                  const SizedBox(height: 28),

                  // File picker
                  Text('Proof of Payment File',
                      style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 10),
                  _FilePicker(
                    fileName: uploadState.selectedFileName,
                    onTap: () => _pickFile(notifier),
                    onClear: notifier.clearFile,
                  ).animate().fadeIn(delay: 200.ms),

                  const SizedBox(height: 12),

                  // Error banner
                  if (uploadState.status == UploadStatus.error &&
                      uploadState.errorMessage != null)
                    Container(
                      margin: const EdgeInsets.only(bottom: 12),
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: AppColors.error.withOpacity(0.08),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(
                            color: AppColors.error.withOpacity(0.3)),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.error_outline_rounded,
                              color: AppColors.error, size: 20),
                          const SizedBox(width: 10),
                          Expanded(
                            child: Text(
                              uploadState.errorMessage!,
                              style: Theme.of(context)
                                  .textTheme
                                  .bodyMedium
                                  ?.copyWith(color: AppColors.error),
                            ),
                          ),
                        ],
                      ),
                    ).animate().fadeIn().shakeX(),

                  const SizedBox(height: 24),

                  // Submit button
                  ElevatedButton(
                    onPressed:
                        uploadState.status == UploadStatus.uploading
                            ? null
                            : () => _submit(notifier, tenantId, reference),
                    child: uploadState.status == UploadStatus.uploading
                        ? const Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                    color: Colors.white, strokeWidth: 2.5),
                              ),
                              SizedBox(width: 12),
                              Text('Uploading…'),
                            ],
                          )
                        : const Text('Submit Proof of Payment'),
                  ).animate().fadeIn(delay: 250.ms),
                ],
              ),
            ),
          ),
        );
      },
      loading: () => const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      ),
      error: (_, __) => Scaffold(
        appBar: AppBar(title: const Text('Upload Proof of Payment')),
        body: const Center(
          child: Text(
            'Could not load your tenant details.\nPlease go back and try again.',
            textAlign: TextAlign.center,
          ),
        ),
      ),
    );
  }
}

// ── File picker widget ────────────────────────────────────────────────────────
class _FilePicker extends StatelessWidget {
  final String? fileName;
  final VoidCallback onTap;
  final VoidCallback onClear;

  const _FilePicker(
      {required this.fileName, required this.onTap, required this.onClear});

  @override
  Widget build(BuildContext context) {
    final hasFile = fileName != null;

    return GestureDetector(
      onTap: hasFile ? null : onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 300),
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: hasFile
              ? AppColors.success.withOpacity(0.05)
              : AppColors.surfaceVariant,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: hasFile ? AppColors.success : AppColors.border,
            width: 1.5,
            style: hasFile ? BorderStyle.solid : BorderStyle.solid,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                color: hasFile
                    ? AppColors.success.withOpacity(0.12)
                    : AppColors.primary.withOpacity(0.08),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(
                hasFile ? Icons.insert_drive_file_rounded : Icons.upload_rounded,
                color: hasFile ? AppColors.success : AppColors.primary,
                size: 24,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    hasFile ? fileName! : 'Tap to select file',
                    style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                          color: hasFile
                              ? AppColors.onBackground
                              : AppColors.primary,
                          fontWeight: FontWeight.w600,
                        ),
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    hasFile ? 'File selected ✓' : 'PDF, JPG or PNG',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
            if (hasFile)
              IconButton(
                onPressed: onClear,
                icon: const Icon(Icons.close_rounded, color: AppColors.textMuted),
              ),
          ],
        ),
      ),
    );
  }
}
