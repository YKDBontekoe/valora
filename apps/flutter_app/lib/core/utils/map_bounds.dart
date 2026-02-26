class MapBounds {
  const MapBounds({
    required this.minLat,
    required this.minLon,
    required this.maxLat,
    required this.maxLon,
  });

  final double minLat;
  final double minLon;
  final double maxLat;
  final double maxLon;

  MapBounds expand(double factor) {
    final latSpan = maxLat - minLat;
    final lonSpan = maxLon - minLon;
    final latPadding = latSpan * factor;
    final lonPadding = lonSpan * factor;
    return MapBounds(
      minLat: minLat - latPadding,
      minLon: minLon - lonPadding,
      maxLat: maxLat + latPadding,
      maxLon: maxLon + lonPadding,
    );
  }

  bool contains(MapBounds other) {
    return other.minLat >= minLat &&
        other.minLon >= minLon &&
        other.maxLat <= maxLat &&
        other.maxLon <= maxLon;
  }

  String cacheKey(int zoomBucket) {
    return '$zoomBucket:${_round(minLat)}:${_round(minLon)}:${_round(maxLat)}:${_round(maxLon)}';
  }

  String _round(double value) => value.toStringAsFixed(3);
}
