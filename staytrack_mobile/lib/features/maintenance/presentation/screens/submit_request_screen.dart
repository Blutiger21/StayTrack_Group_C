import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:staytrack_mobile/core/constants/app_constants.dart';
import 'package:staytrack_mobile/core/theme/app_theme.dart';
import 'package:staytrack_mobile/features/home/presentation/providers/home_provider.dart';
import 'package:staytrack_mobile/features/maintenance/presentation/providers/maintenance_provider.dart';

class SubmitRequestScreen extends ConsumerStatefulWidget {
  const SubmitRequestScreen({super.key});

  @override
  ConsumerState<SubmitRequestScreen> createState() =>
      _SubmitRequestScreenState();
}

class _SubmitRequestScreenState extends ConsumerState<SubmitRequestScreen> {
  final _formKey = GlobalKey<FormState>();
  final _titleCtrl = TextEditingController();
  final _descriptionCtrl = TextEditingController();
  bool _submitted = false;

  @override
  void dispose() {
    _titleCtrl.dispose();
    _descriptionCtrl.dispose();
    super.dispose();
  }

  Future<void> _pickPhoto(MaintenanceSubmitNotifier notifier) async {
    final picker = ImagePicker();
    final source = await showModalBottomSheet<ImageSource>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(height: 12),
            Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                  color: AppColors.border,
                  borderRadius: BorderRadius.circular(2)),
            ),
            const SizedBox(height: 20),
            ListTile(
              leading: const Icon(Icons.camera_alt_rounded, color: AppColors.primary),
              title: const Text('Take a Photo'),
              onTap: () => Navigator.pop(context, ImageSource.camera),
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_rounded, color: AppColors.primary),
              title: const Text('Choose from Gallery'),
              onTap: () => Navigator.pop(context, ImageSource.gallery),
            ),
            const SizedBox(height: 12),
          ],
        ),
      ),
    );

    if (source == null) return;
    final picked = await picker.pickImage(source: source, imageQuality: 80);
    if (picked != null) {
      notifier.setPhoto(File(picked.path), picked.name);
    }
  }

  Future<void> _submit(
      MaintenanceSubmitNotifier notifier, int tenantId) async {
    setState(() => _submitted = true);
    if (!_formKey.currentState!.validate()) return;

    final success = await notifier.submit(
      title: _titleCtrl.text.trim(),
      description: _descriptionCtrl.text.trim(),
    );

    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('✅ Maintenance request submitted successfully!'),
          backgroundColor: AppColors.success,
        ),
      );
      ref.invalidate(maintenanceRequestsProvider);
      ref.invalidate(myMaintenanceRequestsProvider);
      context.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final tenantAsync = ref.watch(tenantProvider);

    return tenantAsync.when(
      data: (tenant) {
        final tenantId = tenant?.tenantId ?? 0;
        final submitState =
            ref.watch(maintenanceSubmitProvider(tenantId));
        final notifier =
            ref.read(maintenanceSubmitProvider(tenantId).notifier);
        final isSubmitting =
            submitState.status == SubmitStatus.submitting;

        return Scaffold(
          appBar: AppBar(title: const Text('Log Maintenance Issue')),
          body: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Form(
              key: _formKey,
              autovalidateMode: _submitted
                  ? AutovalidateMode.onUserInteraction
                  : AutovalidateMode.disabled,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Title
                  _FieldLabel('Issue Title'),
                  const SizedBox(height: 10),
                  TextFormField(
                    controller: _titleCtrl,
                    decoration: const InputDecoration(
                      hintText: 'e.g. Leaking tap in bathroom',
                      prefixIcon: Icon(Icons.title_rounded),
                    ),
                    validator: (v) => (v == null || v.trim().isEmpty)
                        ? 'Please enter a brief title for the issue.'
                        : null,
                  ).animate().fadeIn(delay: 50.ms),

                  const SizedBox(height: 20),

                  // Description
                  _FieldLabel('Description'),
                  const SizedBox(height: 10),
                  TextFormField(
                    controller: _descriptionCtrl,
                    minLines: 4,
                    maxLines: 8,
                    decoration: const InputDecoration(
                      hintText:
                          'Describe the issue in detail. When did it start? Has it happened before?',
                      alignLabelWithHint: true,
                    ),
                    validator: (v) {
                      if (v == null || v.trim().isEmpty) {
                        return 'Please describe the issue.';
                      }
                      if (v.trim().length < 15) {
                        return 'Please provide a more detailed description (at least 15 characters).';
                      }
                      return null;
                    },
                  ).animate().fadeIn(delay: 200.ms),

                  const SizedBox(height: 20),

                  // Optional photo
                  _FieldLabel('Photo (Optional)'),
                  const SizedBox(height: 10),
                  _PhotoPicker(
                    photo: submitState.photo,
                    photoName: submitState.photoName,
                    onPick: () => _pickPhoto(notifier),
                    onClear: notifier.clearPhoto,
                  ).animate().fadeIn(delay: 250.ms),

                  const SizedBox(height: 12),

                  // Error
                  if (submitState.status == SubmitStatus.error &&
                      submitState.errorMessage != null)
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
                              submitState.errorMessage!,
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

                  // Submit
                  ElevatedButton(
                    onPressed: isSubmitting
                        ? null
                        : () => _submit(notifier, tenantId),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.warning,
                    ),
                    child: isSubmitting
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
                              Text('Submitting…'),
                            ],
                          )
                        : const Text('Submit Request'),
                  ).animate().fadeIn(delay: 300.ms),

                  const SizedBox(height: 32),
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
        appBar: AppBar(title: const Text('Log Maintenance Issue')),
        body: const Center(
          child: Text(
            'Could not load your details.\nPlease go back and try again.',
            textAlign: TextAlign.center,
          ),
        ),
      ),
    );
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────
class _FieldLabel extends StatelessWidget {
  final String text;
  const _FieldLabel(this.text);

  @override
  Widget build(BuildContext context) =>
      Text(text, style: Theme.of(context).textTheme.titleMedium);
}

class _UrgencySelector extends StatelessWidget {
  final String selected;
  final ValueChanged<String> onChanged;

  const _UrgencySelector({required this.selected, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: AppConstants.urgencyLevels.map((level) {
        final isSelected = selected == level;
        Color color;
        switch (level) {
          case 'High':
            color = AppColors.error;
            break;
          case 'Medium':
            color = AppColors.warning;
            break;
          default:
            color = AppColors.success;
        }
        return Expanded(
          child: Padding(
            padding: const EdgeInsets.only(right: 8),
            child: GestureDetector(
              onTap: () => onChanged(level),
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                padding: const EdgeInsets.symmetric(vertical: 12),
                decoration: BoxDecoration(
                  color: isSelected
                      ? color.withOpacity(0.12)
                      : AppColors.surfaceVariant,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(
                    color: isSelected ? color : AppColors.border,
                    width: isSelected ? 2 : 1,
                  ),
                ),
                child: Column(
                  children: [
                    Icon(
                      level == 'High'
                          ? Icons.local_fire_department_rounded
                          : level == 'Medium'
                              ? Icons.warning_amber_rounded
                              : Icons.check_circle_outline_rounded,
                      color: isSelected ? color : AppColors.textMuted,
                      size: 22,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      level,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: isSelected
                            ? FontWeight.w700
                            : FontWeight.w500,
                        color: isSelected ? color : AppColors.textMuted,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        );
      }).toList(),
    );
  }
}

class _PhotoPicker extends StatelessWidget {
  final File? photo;
  final String? photoName;
  final VoidCallback onPick;
  final VoidCallback onClear;

  const _PhotoPicker({
    this.photo,
    this.photoName,
    required this.onPick,
    required this.onClear,
  });

  @override
  Widget build(BuildContext context) {
    final hasPhoto = photo != null;

    return GestureDetector(
      onTap: hasPhoto ? null : onPick,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 300),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: hasPhoto
              ? AppColors.primary.withOpacity(0.05)
              : AppColors.surfaceVariant,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: hasPhoto ? AppColors.primary : AppColors.border,
            width: 1.5,
          ),
        ),
        child: hasPhoto
            ? Row(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(10),
                    child: Image.file(
                      photo!,
                      width: 60,
                      height: 60,
                      fit: BoxFit.cover,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          photoName ?? 'Photo selected',
                          style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                                fontWeight: FontWeight.w600,
                              ),
                          overflow: TextOverflow.ellipsis,
                        ),
                        Text('Tap × to remove',
                            style: Theme.of(context).textTheme.bodySmall),
                      ],
                    ),
                  ),
                  IconButton(
                    onPressed: onClear,
                    icon: const Icon(Icons.close_rounded,
                        color: AppColors.textMuted),
                  ),
                ],
              )
            : Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.add_a_photo_rounded,
                      color: AppColors.primary, size: 24),
                  const SizedBox(width: 12),
                  Text(
                    'Add a photo of the issue',
                    style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                          color: AppColors.primary,
                          fontWeight: FontWeight.w600,
                        ),
                  ),
                ],
              ),
      ),
    );
  }
}
