import 'package:uuid/uuid.dart';

import '../models/saved_search.dart';
import '../repositories/saved_search_repository.dart';

class SavedSearchService {
  final SavedSearchRepository _repository;

  SavedSearchService(this._repository);

  Future<List<SavedSearch>> getSavedSearches() {
    return _repository.getSavedSearches();
  }

  Future<SavedSearch> saveSearch(String query, int radiusMeters) async {
    // Check if already exists to update timestamp or prevent dupes?
    // Let's prevent duplicates for same query+radius
    final current = await _repository.getSavedSearches();
    final existingIndex = current.indexWhere(
        (s) => s.query.toLowerCase() == query.toLowerCase() && s.radiusMeters == radiusMeters);

    if (existingIndex != -1) {
      final existing = current[existingIndex];
      // Update createdAt to bump to top? Or just return existing?
      // Let's return existing for now, maybe user wants to toggle alert on it.
      return existing;
    }

    final newSearch = SavedSearch(
      id: const Uuid().v4(),
      query: query,
      radiusMeters: radiusMeters,
      createdAt: DateTime.now(),
      isAlertEnabled: false,
    );

    await _repository.saveSearch(newSearch);
    return newSearch;
  }

  Future<void> removeSearch(String id) {
    return _repository.removeSearch(id);
  }

  Future<void> toggleAlert(String id) async {
    final searches = await _repository.getSavedSearches();
    final index = searches.indexWhere((s) => s.id == id);
    if (index != -1) {
      final search = searches[index];
      final updated = search.copyWith(isAlertEnabled: !search.isAlertEnabled);
      await _repository.updateSearch(updated);
    }
  }
}
