class MapAmenityCluster {
  final double latitude;
  final double longitude;
  final int count;
  final Map<String, int> typeCounts;

  MapAmenityCluster({
    required this.latitude,
    required this.longitude,
    required this.count,
    required this.typeCounts,
  });

  factory MapAmenityCluster.fromJson(Map<String, dynamic> json) {
    return MapAmenityCluster(
      latitude: json['latitude'] as double,
      longitude: json['longitude'] as double,
      count: json['count'] as int,
      typeCounts: Map<String, int>.from(json['typeCounts'] as Map),
    );
  }
}
