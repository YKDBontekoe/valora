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
    final savedSearches = await _repository.getSavedSearches();
    final duplicateSearchIndex = savedSearches.indexWhere(
        (search) => search.query.toLowerCase() == query.toLowerCase() && search.radiusMeters == radiusMeters);

    if (duplicateSearchIndex != -1) {
      final existing = savedSearches[duplicateSearchIndex];
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
    final index = searches.indexWhere((search) => search.id == id);
    if (index != -1) {
      final search = searches[index];
      final updated = search.copyWith(isAlertEnabled: !search.isAlertEnabled);
      await _repository.updateSearch(updated);
    }
  }
}
