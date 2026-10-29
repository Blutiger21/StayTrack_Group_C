import 'package:stay_track/core/models/room.dart';

class Tenant {
  final int tenantId;
  final String userId;       // FK → auth.users.id (Guid/UUID)
  final int? roomId;
  final String studentNumber;
  final String? paymentReference;
  final double rentalAmount;
  final DateTime? leaseStart;
  final DateTime? leaseEnd;
  final String status;
  final Room? room;

  const Tenant({
    required this.tenantId,
    required this.userId,
    this.roomId,
    required this.studentNumber,
    this.paymentReference,
    required this.rentalAmount,
    this.leaseStart,
    this.leaseEnd,
    this.status = 'active',
    this.room,
  });

  factory Tenant.fromMap(Map<String, dynamic> map) => Tenant(
        tenantId: (map['tenant_id'] as num).toInt(),
        userId: map['user_id'] as String? ?? '',
        roomId: map['room_id'] != null ? (map['room_id'] as num).toInt() : null,
        studentNumber: map['student_number'] as String? ?? '',
        paymentReference: map['payment_reference'] as String?,
        rentalAmount: (map['rental_amount'] as num?)?.toDouble() ?? 0.0,
        leaseStart: map['lease_start'] != null
            ? DateTime.tryParse(map['lease_start'] as String)
            : null,
        leaseEnd: map['lease_end'] != null
            ? DateTime.tryParse(map['lease_end'] as String)
            : null,
        status: map['status'] as String? ?? 'active',
        room: map['room'] != null
            ? Room.fromMap(map['room'] as Map<String, dynamic>)
            : map['rooms'] != null
                ? Room.fromMap(map['rooms'] as Map<String, dynamic>)
                : null,
      );
}
