import 'package:equatable/equatable.dart';

class SavedProperty extends Equatable {
  final String id;
  final String userId;
  final String address;
  final double latitude;
  final double longitude;
  final String? cachedScore;
  final DateTime createdAt;

  const SavedProperty({
    required this.id,
    required this.userId,
    required this.address,
    required this.latitude,
    required this.longitude,
    this.cachedScore,
    required this.createdAt,
  });

  factory SavedProperty.fromJson(Map<String, dynamic> json) {
    return SavedProperty(
      id: json['id'] as String,
      userId: json['userId'] as String,
      address: json['address'] as String,
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      cachedScore: json['cachedScore'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'address': address,
      'latitude': latitude,
      'longitude': longitude,
      'cachedScore': cachedScore,
      'createdAt': createdAt.toIso8601String(),
    };
  }

  @override
  List<Object?> get props => [id, userId, address, latitude, longitude, cachedScore, createdAt];
}
