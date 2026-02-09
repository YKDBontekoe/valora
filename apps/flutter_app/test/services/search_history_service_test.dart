import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/services/search_history_service.dart';

void main() {
  group('SearchHistoryService', () {
    late SearchHistoryService service;

    setUp(() {
      SharedPreferences.setMockInitialValues({});
      service = SearchHistoryService();
    });

    test('getHistory returns empty list initially', () async {
      final history = await service.getHistory();
      expect(history, isEmpty);
    });

    test('addToHistory adds item', () async {
      await service.addToHistory('test query');
      final history = await service.getHistory();
      expect(history.length, 1);
      expect(history.first.query, 'test query');
    });

    test('addToHistory puts latest item at top', () async {
      await service.addToHistory('first');
      await service.addToHistory('second');
      final history = await service.getHistory();
      expect(history.length, 2);
      expect(history[0].query, 'second');
      expect(history[1].query, 'first');
    });

    test('addToHistory avoids duplicates and moves to top', () async {
      await service.addToHistory('first');
      await service.addToHistory('second');
      await service.addToHistory('first'); // Should move to top
      final history = await service.getHistory();
      expect(history.length, 2);
      expect(history[0].query, 'first');
      expect(history[1].query, 'second');
    });

    test('addToHistory limits history to 10', () async {
      for (int i = 0; i < 15; i++) {
        await service.addToHistory('query $i');
      }
      final history = await service.getHistory();
      expect(history.length, 10);
      expect(history.first.query, 'query 14'); // Latest
      expect(history.last.query, 'query 5');   // Oldest kept
    });

    test('removeFromHistory removes item', () async {
      await service.addToHistory('keep');
      await service.addToHistory('remove');
      await service.removeFromHistory('remove');
      final history = await service.getHistory();
      expect(history.length, 1);
      expect(history.first.query, 'keep');
    });

    test('clearHistory removes all items', () async {
      await service.addToHistory('one');
      await service.addToHistory('two');
      await service.clearHistory();
      final history = await service.getHistory();
      expect(history, isEmpty);
    });
  });
}
