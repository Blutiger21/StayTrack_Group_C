/// Represents a room inside a property.
class Room {
  final int roomId;
  final int? propertyId;
  final String roomNumber;
  final String? roomType;
  final String status; // vacant | occupied

  const Room({
    required this.roomId,
    this.propertyId,
    required this.roomNumber,
    this.roomType,
    this.status = 'vacant',
  });

  factory Room.fromMap(Map<String, dynamic> map) => Room(
        roomId: (map['room_id'] as num).toInt(),
        propertyId: map['property_id'] != null
            ? (map['property_id'] as num).toInt()
            : null,
        roomNumber: map['room_number'] as String? ?? '',
        roomType: map['room_type'] as String?,
        status: map['status'] as String? ?? 'vacant',
      );
}
