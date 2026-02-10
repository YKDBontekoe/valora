import 'package:latlong2/latlong.dart';

class MapCityInsight {
  final String city;
  final int count;
  final LatLng location;
  final double? compositeScore;
  final double? safetyScore;
  final double? socialScore;
  final double? amenitiesScore;

  MapCityInsight({
    required this.city,
    required this.count,
    required this.location,
    this.compositeScore,
    this.safetyScore,
    this.socialScore,
    this.amenitiesScore,
  });

  factory MapCityInsight.fromJson(Map<String, dynamic> json) {
    return MapCityInsight(
      city: json['city'] as String,
      count: json['count'] as int,
      location: LatLng(
        (json['latitude'] as num).toDouble(),
        (json['longitude'] as num).toDouble(),
      ),
      compositeScore: (json['compositeScore'] as num?)?.toDouble(),
      safetyScore: (json['safetyScore'] as num?)?.toDouble(),
      socialScore: (json['socialScore'] as num?)?.toDouble(),
      amenitiesScore: (json['amenitiesScore'] as num?)?.toDouble(),
    );
  }
}
