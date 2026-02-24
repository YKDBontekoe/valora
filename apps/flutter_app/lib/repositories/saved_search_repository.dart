import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/saved_search.dart';

abstract class SavedSearchRepository {
  Future<List<SavedSearch>> getSavedSearches();
  Future<void> saveSearch(SavedSearch search);
  Future<void> removeSearch(String id);
  Future<void> updateSearch(SavedSearch search);
}

class LocalSavedSearchRepository implements SavedSearchRepository {
  static const String _key = 'saved_searches';

  @override
  Future<List<SavedSearch>> getSavedSearches() async {
    final prefs = await SharedPreferences.getInstance();
    final String? jsonString = prefs.getString(_key);
    if (jsonString == null) {
      return [];
    }

    try {
      final List<dynamic> jsonList = json.decode(jsonString);
      return jsonList
          .map((e) => SavedSearch.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (e) {
      return [];
    }
  }

  @override
  Future<void> saveSearch(SavedSearch search) async {
    final searches = await getSavedSearches();

    // Check if already exists (by ID) and update, or add new
    final index = searches.indexWhere((s) => s.id == search.id);
    if (index != -1) {
      searches[index] = search;
    } else {
      // Add to top
      searches.insert(0, search);
    }

    await _saveList(searches);
  }

  @override
  Future<void> removeSearch(String id) async {
    final searches = await getSavedSearches();
    searches.removeWhere((s) => s.id == id);
    await _saveList(searches);
  }

  @override
  Future<void> updateSearch(SavedSearch search) async {
    // Re-use saveSearch as it handles update by ID
    await saveSearch(search);
  }

  Future<void> _saveList(List<SavedSearch> searches) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(
      _key,
      json.encode(searches.map((e) => e.toJson()).toList()),
    );
  }
}
