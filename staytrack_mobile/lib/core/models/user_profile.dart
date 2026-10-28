class UserProfile {
  final String id;
  final String fullName;
  final String email;
  final String? phone;

  const UserProfile({
    required this.id,
    required this.fullName,
    required this.email,
    this.phone,
  });

  factory UserProfile.fromMap(Map<String, dynamic> map) => UserProfile(
        id: map['id'] as String,
        fullName: map['full_name'] as String? ?? 'Student',
        email: map['email'] as String? ?? '',
        phone: map['phone'] as String?,
      );

  Map<String, dynamic> toMap() => {
        'id': id,
        'full_name': fullName,
        'email': email,
        if (phone != null) 'phone': phone,
      };

  UserProfile copyWith({
    String? fullName,
    String? phone,
  }) =>
      UserProfile(
        id: id,
        fullName: fullName ?? this.fullName,
        email: email,
        phone: phone ?? this.phone,
      );
}
