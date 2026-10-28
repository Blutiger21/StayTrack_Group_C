import 'package:stay_track/core/constants/app_constants.dart';

class Payment {
  final int paymentId;
  final int tenantId;
  final double amount;
  final DateTime? paymentDate;
  final DateTime? periodMonth;
  final String reference;
  final String status; // pending | Verified | Rejected
  final String? proofUrl;
  final DateTime? verifiedAt;

  const Payment({
    required this.paymentId,
    required this.tenantId,
    required this.amount,
    this.paymentDate,
    this.periodMonth,
    required this.reference,
    required this.status,
    this.proofUrl,
    this.verifiedAt,
  });

  factory Payment.fromMap(Map<String, dynamic> map) => Payment(
        paymentId: (map['payment_id'] as num).toInt(),
        tenantId: (map['tenant_id'] as num).toInt(),
        amount: (map['amount'] as num?)?.toDouble() ?? 0.0,
        paymentDate: map['payment_date'] != null
            ? DateTime.tryParse(map['payment_date'] as String)
            : null,
        periodMonth: map['period_month'] != null
            ? DateTime.tryParse(map['period_month'] as String)
            : null,
        reference: map['reference'] as String? ?? '',
        status: map['status'] as String? ?? AppConstants.paymentPending,
        proofUrl: map['proof_url'] as String?,
        verifiedAt: map['verified_at'] != null
            ? DateTime.tryParse(map['verified_at'] as String)
            : null,
      );

  /// Human-readable month label e.g. "March 2025"
  String get periodLabel {
    if (periodMonth == null) return '';
    const months = [
      'January', 'February', 'March', 'April', 'May', 'June',
      'July', 'August', 'September', 'October', 'November', 'December'
    ];
    return '${months[periodMonth!.month - 1]} ${periodMonth!.year}';
  }

  bool get isVerified => status.toLowerCase() == AppConstants.paymentVerified.toLowerCase();
  bool get isRejected => status.toLowerCase() == AppConstants.paymentRejected.toLowerCase();
  bool get isPending  => !isVerified && !isRejected;
}
