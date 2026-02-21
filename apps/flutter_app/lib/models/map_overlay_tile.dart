class MapOverlayTile {
  final double latitude;
  final double longitude;
  final double size;
  final double value;
  final String displayValue;

  MapOverlayTile({
    required this.latitude,
    required this.longitude,
    required this.size,
    required this.value,
    required this.displayValue,
  });

  factory MapOverlayTile.fromJson(Map<String, dynamic> json) {
    return MapOverlayTile(
      latitude: json['latitude'] as double,
      longitude: json['longitude'] as double,
      size: json['size'] as double,
      value: (json['value'] as num).toDouble(),
      displayValue: json['displayValue'] as String,
    );
  }
}
