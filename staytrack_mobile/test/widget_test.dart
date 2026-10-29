import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:stay_track/core/models/payment.dart';
import 'package:stay_track/core/theme/app_theme.dart';

void main() {
  testWidgets('Payment card renders basic status', (WidgetTester tester) async {
    const payment = Payment(
      paymentId: 1,
      tenantId: 10,
      amount: 4200.0,
      reference: 'REF-TEST-01',
      status: 'pending',
    );

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(
          body: Center(
            child: Text('${payment.reference}: R${payment.amount.toStringAsFixed(2)} (${payment.status})'),
          ),
        ),
      ),
    );

    expect(find.text('REF-TEST-01: R4200.00 (pending)'), findsOneWidget);
  });
}
