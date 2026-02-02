import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/listing.dart';

class FavoritesProvider extends ChangeNotifier {
  List<Listing> _favorites = [];
  bool _isLoading = true;

  List<Listing> get favorites => _favorites;
  bool get isLoading => _isLoading;

  FavoritesProvider() {
    loadFavorites();
  }

  Future<void> loadFavorites() async {
    _isLoading = true;
    notifyListeners();

    try {
      final prefs = await SharedPreferences.getInstance();
      final favoritesJson = prefs.getStringList('favorite_listings') ?? [];

      _favorites = favoritesJson
          .map((item) => Listing.fromJson(json.decode(item)))
          .toList();
    } catch (e) {
      debugPrint('Error loading favorites: $e');
      // In case of error (e.g. model change), clear favorites or handle gracefully
      _favorites = [];
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  bool isFavorite(String id) {
    return _favorites.any((item) => item.id == id);
  }

  Future<void> toggleFavorite(Listing listing) async {
    final existingIndex = _favorites.indexWhere((item) => item.id == listing.id);

    if (existingIndex >= 0) {
      _favorites.removeAt(existingIndex);
    } else {
      _favorites.add(listing);
    }

    notifyListeners();

    try {
      final prefs = await SharedPreferences.getInstance();
      final favoritesJson = _favorites
          .map((item) => json.encode(item.toJson()))
          .toList();

      await prefs.setStringList('favorite_listings', favoritesJson);
    } catch (e) {
      debugPrint('Error saving favorites: $e');
    }
  }
}
