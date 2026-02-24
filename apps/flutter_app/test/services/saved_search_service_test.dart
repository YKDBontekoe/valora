import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/saved_search.dart';
import 'package:valora_app/repositories/saved_search_repository.dart';
import 'package:valora_app/services/saved_search_service.dart';

class MockSavedSearchRepository implements SavedSearchRepository {
  List<SavedSearch> items = [];

  @override
  Future<List<SavedSearch>> getSavedSearches() async => List.from(items);

  @override
  Future<void> saveSearch(SavedSearch search) async {
    final index = items.indexWhere((s) => s.id == search.id);
    if (index != -1) {
      items[index] = search;
    } else {
      items.insert(0, search);
    }
  }

  @override
  Future<void> removeSearch(String id) async {
    items.removeWhere((s) => s.id == id);
  }

  @override
  Future<void> updateSearch(SavedSearch search) async {
    await saveSearch(search);
  }
}

void main() {
  late SavedSearchService service;
  late MockSavedSearchRepository repository;

  setUp(() {
    repository = MockSavedSearchRepository();
    service = SavedSearchService(repository);
  });

  test('saveSearch adds a new search', () async {
    final search = await service.saveSearch('Amsterdam', 1000);
    expect(search.query, 'Amsterdam');
    expect(search.radiusMeters, 1000);
    expect(repository.items.length, 1);
  });

  test('saveSearch returns existing search if query and radius match', () async {
    final s1 = await service.saveSearch('Utrecht', 500);
    final s2 = await service.saveSearch('utrecht', 500);

    expect(s1.id, s2.id);
    expect(repository.items.length, 1);
  });

  test('toggleAlert updates isAlertEnabled', () async {
    final s1 = await service.saveSearch('Rotterdam', 1000);
    expect(s1.isAlertEnabled, false);

    await service.toggleAlert(s1.id);
    expect(repository.items.first.isAlertEnabled, true);

    await service.toggleAlert(s1.id);
    expect(repository.items.first.isAlertEnabled, false);
  });

  test('removeSearch removes the item', () async {
    final s1 = await service.saveSearch('Den Haag', 1000);
    expect(repository.items.length, 1);

    await service.removeSearch(s1.id);
    expect(repository.items.length, 0);
  });
}
