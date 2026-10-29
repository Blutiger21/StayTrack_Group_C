class Announcement {
  final int announcementId;
  final String title;
  final String message;
  final DateTime createdAt;
  final String? createdByName; // from joined profiles.full_name

  const Announcement({
    required this.announcementId,
    required this.title,
    required this.message,
    required this.createdAt,
    this.createdByName,
  });

  factory Announcement.fromMap(Map<String, dynamic> map) => Announcement(
        announcementId: (map['announcement_id'] as num).toInt(),
        title: map['title'] as String? ?? '',
        message: map['message'] as String? ?? '',
        createdAt: map['created_at'] != null
            ? DateTime.tryParse(map['created_at'] as String) ?? DateTime.now()
            : DateTime.now(),
        createdByName: (map['profiles'] as Map<String, dynamic>?)?['full_name'] as String?
            ?? (map['creator'] as Map<String, dynamic>?)?['full_name'] as String?,
      );
}
