import 'package:flutter_test/flutter_test.dart';
import 'package:stay_track/core/models/payment.dart';

void main() {
  group('Payment Model Tests', () {
    test('creates Payment correctly from Supabase map', () {
      final map = {
        'payment_id': 101,
        'tenant_id': 5,
        'amount': 3500.0,
        'payment_date': '2026-03-01T10:00:00.000Z',
        'period_month': '2026-03-01T00:00:00.000Z',
        'reference': 'ST-ROOM-12',
        'status': 'pending',
        'proof_url': 'proof-of-payment/5_20260301_12345.pdf',
        'verified_at': null,
      };

      final payment = Payment.fromMap(map);

      expect(payment.paymentId, 101);
      expect(payment.tenantId, 5);
      expect(payment.amount, 3500.0);
      expect(payment.reference, 'ST-ROOM-12');
      expect(payment.status, 'pending');
      expect(payment.isPending, isTrue);
      expect(payment.isVerified, isFalse);
      expect(payment.isRejected, isFalse);
      expect(payment.periodLabel, 'March 2026');
      expect(payment.proofUrl, 'proof-of-payment/5_20260301_12345.pdf');
    });

    test('verified status behaves correctly', () {
      const payment = Payment(
        paymentId: 1,
        tenantId: 2,
        amount: 2500,
        reference: 'REF1',
        status: 'Verified',
      );

      expect(payment.isVerified, isTrue);
      expect(payment.isPending, isFalse);
      expect(payment.isRejected, isFalse);
    });

    test('rejected status behaves correctly', () {
      const payment = Payment(
        paymentId: 1,
        tenantId: 2,
        amount: 2500,
        reference: 'REF1',
        status: 'Rejected',
      );

      expect(payment.isRejected, isTrue);
      expect(payment.isPending, isFalse);
      expect(payment.isVerified, isFalse);
    });
  });
}
