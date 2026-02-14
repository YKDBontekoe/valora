class MapOverlay {
  final String id;
  final String name;
  final String metricName;
  final double metricValue;
  final String displayValue;
  final Map<String, dynamic> geoJson;

  MapOverlay({
    required this.id,
    required this.name,
    required this.metricName,
    required this.metricValue,
    required this.displayValue,
    required this.geoJson,
  });

  factory MapOverlay.fromJson(Map<String, dynamic> json) {
    return MapOverlay(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      metricName: json['metricName'] ?? '',
      metricValue: (json['metricValue'] as num).toDouble(),
      displayValue: json['displayValue'] ?? '',
      geoJson: json['geoJson'] ?? {},
    );
  }
}

enum MapOverlayMetric {
  pricePerSquareMeter,
  crimeRate,
  populationDensity,
  averageWoz,
}
