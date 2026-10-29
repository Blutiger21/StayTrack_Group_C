import 'package:staytrack_mobile/core/constants/app_constants.dart';

class MaintenanceRequest {
  final int requestId;
  final int tenantId;
  final int? technicianId;
  final String title;
  final String description;
  final String? photoUrl;
  final String status;         // Pending | In Progress | Resolved
  final DateTime dateSubmitted;
  final DateTime? dateCompleted;

  const MaintenanceRequest({
    required this.requestId,
    required this.tenantId,
    this.technicianId,
    required this.title,
    required this.description,
    this.photoUrl,
    required this.status,
    required this.dateSubmitted,
    this.dateCompleted,
  });

  factory MaintenanceRequest.fromMap(Map<String, dynamic> map) =>
      MaintenanceRequest(
        requestId: (map['request_id'] as num).toInt(),
        tenantId: (map['tenant_id'] as num).toInt(),
        technicianId: map['technician_id'] != null
            ? (map['technician_id'] as num).toInt()
            : null,
        title: map['title'] as String? ?? '',
        description: map['description'] as String? ?? '',
        photoUrl: map['photo_url'] as String?,
        status: map['status'] as String? ?? AppConstants.statusPending,
        dateSubmitted: map['date_submitted'] != null
            ? DateTime.tryParse(map['date_submitted'] as String) ?? DateTime.now()
            : DateTime.now(),
        dateCompleted: map['date_completed'] != null
            ? DateTime.tryParse(map['date_completed'] as String)
            : null,
      );

  bool get isResolved => status.toLowerCase() == AppConstants.statusResolved.toLowerCase();
  bool get isInProgress => status.toLowerCase() == AppConstants.statusInProgress.toLowerCase();
  bool get isPending => !isResolved && !isInProgress;
}
