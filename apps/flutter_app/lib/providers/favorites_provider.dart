import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/listing.dart';

class FavoritesProvider extends ChangeNotifier {
  static const String _legacyStorageKey = 'favorite_listings';
  static const String _storageKey = 'favorite_listings_v2';

  List<Listing> _favorites = [];
  final Map<String, DateTime> _savedAtByListingId = <String, DateTime>{};
  bool _isLoading = true;

  List<Listing> get favorites => _favorites;
  bool get isLoading => _isLoading;
  DateTime? savedAtFor(String listingId) => _savedAtByListingId[listingId];

  FavoritesProvider() {
    loadFavorites();
  }

  Future<void> loadFavorites() async {
    _isLoading = true;
    notifyListeners();

    try {
      final prefs = await SharedPreferences.getInstance();
      final favoritesJson = prefs.getStringList(_storageKey);

      if (favoritesJson != null && favoritesJson.isNotEmpty) {
        _loadV2Records(favoritesJson);
      } else {
        _loadLegacyRecords(prefs.getStringList(_legacyStorageKey) ?? []);
      }
    } catch (e) {
      debugPrint('Error loading favorites: $e');
      _favorites = [];
      _savedAtByListingId.clear();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  bool isFavorite(String id) {
    return _favorites.any((item) => item.id == id);
  }

  Future<void> toggleFavorite(Listing listing) async {
    final existingIndex = _favorites.indexWhere(
      (item) => item.id == listing.id,
    );

    if (existingIndex >= 0) {
      _savedAtByListingId.remove(listing.id);
      _favorites.removeAt(existingIndex);
    } else {
      _favorites.add(_toStoredSummary(listing));
      _savedAtByListingId[listing.id] = DateTime.now().toUtc();
    }

    notifyListeners();

    try {
      final prefs = await SharedPreferences.getInstance();
      final favoritesJson = _favorites
          .map((item) => json.encode(_toStorageRecord(item)))
          .toList();

      await prefs.setStringList(_storageKey, favoritesJson);
      await prefs.remove(_legacyStorageKey);
    } catch (e) {
      debugPrint('Error saving favorites: $e');
    }
  }

  Future<void> removeFavorites(List<Listing> listings) async {
    bool changed = false;
    for (final listing in listings) {
      final existingIndex = _favorites.indexWhere(
        (item) => item.id == listing.id,
      );
      if (existingIndex >= 0) {
        _savedAtByListingId.remove(listing.id);
        _favorites.removeAt(existingIndex);
        changed = true;
      }
    }

    if (!changed) return;

    notifyListeners();

    try {
      final prefs = await SharedPreferences.getInstance();
      final favoritesJson = _favorites
          .map((item) => json.encode(_toStorageRecord(item)))
          .toList();

      await prefs.setStringList(_storageKey, favoritesJson);
      await prefs.remove(_legacyStorageKey);
    } catch (e) {
      debugPrint('Error saving favorites: $e');
    }
  }

  void _loadV2Records(List<String> favoritesJson) {
    _favorites = <Listing>[];
    _savedAtByListingId.clear();

    for (final String item in favoritesJson) {
      final dynamic decoded = json.decode(item);
      if (decoded is! Map<String, dynamic>) {
        continue;
      }

      final dynamic listingJson = decoded['listing'];
      if (listingJson is! Map<String, dynamic>) {
        continue;
      }

      final Listing listing = Listing.fromJson(listingJson);
      _favorites.add(listing);

      final String? savedAtRaw = decoded['savedAt'] as String?;
      final DateTime savedAt =
          DateTime.tryParse(savedAtRaw ?? '')?.toUtc() ??
          DateTime.now().toUtc();
      _savedAtByListingId[listing.id] = savedAt;
    }
  }

  void _loadLegacyRecords(List<String> favoritesJson) {
    _favorites = favoritesJson
        .map(
          (item) => Listing.fromJson(json.decode(item) as Map<String, dynamic>),
        )
        .map(_toStoredSummary)
        .toList();
    _savedAtByListingId
      ..clear()
      ..addEntries(
        _favorites.map(
          (listing) =>
              MapEntry<String, DateTime>(listing.id, DateTime.now().toUtc()),
        ),
      );
  }

  Map<String, dynamic> _toStorageRecord(Listing listing) {
    final DateTime savedAt =
        _savedAtByListingId[listing.id] ?? DateTime.now().toUtc();
    return <String, dynamic>{
      'savedAt': savedAt.toIso8601String(),
      'listing': _toStoredSummary(listing).toJson(),
    };
  }

  Listing _toStoredSummary(Listing listing) {
    return Listing(
      id: listing.id,
      fundaId: listing.fundaId,
      address: listing.address,
      city: listing.city,
      postalCode: listing.postalCode,
      price: listing.price,
      bedrooms: listing.bedrooms,
      bathrooms: listing.bathrooms,
      livingAreaM2: listing.livingAreaM2,
      propertyType: listing.propertyType,
      status: listing.status,
      url: listing.url,
      imageUrl: listing.imageUrl,
      listedDate: listing.listedDate,
      createdAt: listing.createdAt,
      energyLabel: listing.energyLabel,
      contextCompositeScore: listing.contextCompositeScore,
      contextSafetyScore: listing.contextSafetyScore,
    );
  }
}
