import 'dart:async';

import '../core/utils/map_bounds.dart';
import '../models/map_amenity.dart';
import '../models/map_amenity_cluster.dart';
import '../models/map_overlay.dart';
import '../models/map_overlay_tile.dart';
import '../repositories/map_repository.dart';

/// Provides map layer data with:
///   - In-flight deduplication — one HTTP request for concurrent identical lookups
///   - LRU-bounded in-memory cache — never grows past [_maxEntries] per layer
///   - Per-layer TTL expiry — stale tiles are silently evicted on next read
class MapLayerService {
  final MapRepository _repository;

  // Bounded caches (LRU eviction once max capacity is reached)
  final _LruCache<String, List<MapAmenity>>         _amenitiesCache;
  final _LruCache<String, List<MapAmenityCluster>>  _amenityClustersCache;
  final _LruCache<String, List<MapOverlay>>         _overlaysCache;
  final _LruCache<String, List<MapOverlayTile>>     _overlayTilesCache;

  // In-flight trackers — prevent duplicate concurrent HTTP calls
  final Map<String, Future<List<MapAmenity>>>         _amenitiesInflight = {};
  final Map<String, Future<List<MapAmenityCluster>>>  _amenityClustersInflight = {};
  final Map<String, Future<List<MapOverlay>>>         _overlaysInflight = {};
  final Map<String, Future<List<MapOverlayTile>>>     _overlayTilesInflight = {};

  static const int    _maxEntries      = 64;
  static const Duration _amenitiesTtl     = Duration(minutes: 3);
  static const Duration _overlaysTtl      = Duration(minutes: 10);

  MapLayerService(this._repository)
      : _amenitiesCache       = _LruCache(_maxEntries, _amenitiesTtl),
        _amenityClustersCache = _LruCache(_maxEntries, _amenitiesTtl),
        _overlaysCache        = _LruCache(_maxEntries, _overlaysTtl),
        _overlayTilesCache    = _LruCache(_maxEntries, _overlaysTtl);

  Future<List<MapAmenity>> getAmenities(MapBounds bounds, int zoomBucket) async {
    final key = 'amenities:${bounds.cacheKey(zoomBucket)}';
    final hit = _amenitiesCache.get(key);
    if (hit != null) return hit;

    return _amenitiesInflight.putIfAbsent(key, () async {
      try {
        final result = await _repository.getMapAmenities(
          minLat: bounds.minLat,
          minLon: bounds.minLon,
          maxLat: bounds.maxLat,
          maxLon: bounds.maxLon,
        );
        _amenitiesCache.put(key, result);
        return result;
      } finally {
        _amenitiesInflight.remove(key);
      }
    });
  }

  Future<List<MapAmenityCluster>> getAmenityClusters(
    MapBounds bounds,
    int zoomBucket,
    double zoom,
  ) async {
    final key = 'amenity_clusters:${bounds.cacheKey(zoomBucket)}';
    final hit = _amenityClustersCache.get(key);
    if (hit != null) return hit;

    return _amenityClustersInflight.putIfAbsent(key, () async {
      try {
        final result = await _repository.getMapAmenityClusters(
          minLat: bounds.minLat,
          minLon: bounds.minLon,
          maxLat: bounds.maxLat,
          maxLon: bounds.maxLon,
          zoom: zoom,
        );
        _amenityClustersCache.put(key, result);
        return result;
      } finally {
        _amenityClustersInflight.remove(key);
      }
    });
  }

  Future<List<MapOverlay>> getOverlays(
    MapBounds bounds,
    int zoomBucket,
    String metric,
  ) async {
    final key = 'overlays:$metric:${bounds.cacheKey(zoomBucket)}';
    final hit = _overlaysCache.get(key);
    if (hit != null) return hit;

    return _overlaysInflight.putIfAbsent(key, () async {
      try {
        final result = await _repository.getMapOverlays(
          minLat: bounds.minLat,
          minLon: bounds.minLon,
          maxLat: bounds.maxLat,
          maxLon: bounds.maxLon,
          metric: metric,
        );
        _overlaysCache.put(key, result);
        return result;
      } finally {
        _overlaysInflight.remove(key);
      }
    });
  }

  Future<List<MapOverlayTile>> getOverlayTiles(
    MapBounds bounds,
    int zoomBucket,
    double zoom,
    String metric,
  ) async {
    final key = 'overlay_tiles:$metric:${bounds.cacheKey(zoomBucket)}';
    final hit = _overlayTilesCache.get(key);
    if (hit != null) return hit;

    return _overlayTilesInflight.putIfAbsent(key, () async {
      try {
        final result = await _repository.getMapOverlayTiles(
          minLat: bounds.minLat,
          minLon: bounds.minLon,
          maxLat: bounds.maxLat,
          maxLon: bounds.maxLon,
          zoom: zoom,
          metric: metric,
        );
        _overlayTilesCache.put(key, result);
        return result;
      } finally {
        _overlayTilesInflight.remove(key);
      }
    });
  }

  /// Removes all cached data (useful when auth changes or user logs out).
  void clearAll() {
    _amenitiesCache.clear();
    _amenityClustersCache.clear();
    _overlaysCache.clear();
    _overlayTilesCache.clear();
  }
}

// ---------------------------------------------------------------------------
// Minimal LRU cache with TTL
// ---------------------------------------------------------------------------

class _CacheEntry<V> {
  final V value;
  final DateTime expiresAt;

  _CacheEntry(this.value, Duration ttl) : expiresAt = DateTime.now().add(ttl);

  bool get isExpired => DateTime.now().isAfter(expiresAt);
}

/// Simple LRU cache backed by a [LinkedHashMap]-like insertion-ordered [Map].
/// When [maxSize] is reached, the least-recently-used (first inserted/accessed)
/// entry is evicted. Entries older than [ttl] are treated as misses.
class _LruCache<K, V> {
  final int maxSize;
  final Duration ttl;
  final Map<K, _CacheEntry<V>> _map = {};

  _LruCache(this.maxSize, this.ttl);

  V? get(K key) {
    final entry = _map[key];
    if (entry == null) return null;
    if (entry.isExpired) {
      _map.remove(key);
      return null;
    }
    // Refresh LRU position by re-inserting
    _map.remove(key);
    _map[key] = entry;
    return entry.value;
  }

  void put(K key, V value) {
    _map.remove(key); // Remove old entry to refresh position
    if (_map.length >= maxSize) {
      // Evict the oldest (first) entry
      _map.remove(_map.keys.first);
    }
    _map[key] = _CacheEntry(value, ttl);
  }

  void clear() => _map.clear();
}
