import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/models/search_history_item.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/search_history_service.dart';

// Mock ApiService
class _MockApiService extends ApiService {
  final Map<String, ContextReport> reports = {};
  bool shouldFail = false;

  @override
  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    if (shouldFail) {
      throw Exception('API Failed');
    }
    if (reports.containsKey(input)) {
      return reports[input]!;
    }
    throw Exception('Report not found for $input');
  }
}

// Mock SearchHistoryService (not strictly needed but good to have a simple one)
class _MockHistoryService extends SearchHistoryService {
  @override
  Future<List<SearchHistoryItem>> getHistory() async => [];

  @override
  Future<void> addToHistory(String query) async {}

  @override
  Future<void> clearHistory() async {}
}

void main() {
  late ContextReportProvider provider;
  late _MockApiService apiService;

  ContextReport createReport(String query, double score) {
    return ContextReport(
      location: ContextLocation(
        query: query,
        displayAddress: '$query address',
        latitude: 0,
        longitude: 0,
      ),
      socialMetrics: [],
      crimeMetrics: [],
      demographicsMetrics: [],
      housingMetrics: [],
      mobilityMetrics: [],
      amenityMetrics: [],
      environmentMetrics: [],
      compositeScore: score,
      categoryScores: {},
      sources: [],
      warnings: [],
    );
  }

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    apiService = _MockApiService();
    // Pre-populate API service
    apiService.reports['A'] = createReport('A', 80);
    apiService.reports['B'] = createReport('B', 90);

    provider = ContextReportProvider(
      apiService: apiService,
      historyService: _MockHistoryService(),
    );
  });

  group('Comparison Logic', () {
    test('addToComparison adds report ID', () async {
      await provider.addToComparison('A', 1000);

      expect(provider.comparisonIds.length, 1);
      expect(provider.isComparing('A', 1000), isTrue);
    });

    test('addToComparison does not add duplicate ID', () async {
      await provider.addToComparison('A', 1000);
      await provider.addToComparison('A', 1000);

      expect(provider.comparisonIds.length, 1);
    });

    test('addToComparison fetches report if missing', () async {
      // Ensure activeReports is empty initially
      expect(provider.getReportById('A|1000'), isNull);

      await provider.addToComparison('A', 1000);

      // Should have fetched
      expect(provider.getReportById('A|1000'), isNotNull);
      expect(provider.getReportById('A|1000')!.compositeScore, 80);
    });

    test('addToComparison handles fetch failure gracefully', () async {
      apiService.shouldFail = true;

      await provider.addToComparison('A', 1000);

      expect(provider.isComparing('A', 1000), isTrue); // ID added
      expect(provider.getReportById('A|1000'), isNull); // Report not fetched
    });

    test('removeFromComparison removes ID', () async {
      await provider.addToComparison('A', 1000);
      expect(provider.isComparing('A', 1000), isTrue);

      provider.removeFromComparison('A', 1000);
      expect(provider.isComparing('A', 1000), isFalse);
    });

    test('removeFromComparison does nothing if ID not present', () async {
      provider.removeFromComparison('A', 1000);
      expect(provider.comparisonIds, isEmpty);
    });

    test('toggleComparison toggles state', () async {
      await provider.toggleComparison('A', 1000);
      expect(provider.isComparing('A', 1000), isTrue);

      await provider.toggleComparison('A', 1000);
      expect(provider.isComparing('A', 1000), isFalse);
    });

    test('clearComparison clears all', () async {
      await provider.addToComparison('A', 1000);
      await provider.addToComparison('B', 1000);
      expect(provider.comparisonIds.length, 2);

      provider.clearComparison();
      expect(provider.comparisonIds, isEmpty);
    });

    test('clearComparison does nothing if empty', () async {
      provider.clearComparison();
      expect(provider.comparisonIds, isEmpty);
    });

    test('comparisonReports returns loaded reports', () async {
      await provider.addToComparison('A', 1000);
      await provider.addToComparison('B', 1000);

      final reports = provider.comparisonReports;
      expect(reports.length, 2);
    });

    test('persistence works', () async {
      await provider.addToComparison('A', 1000);

      // Verify prefs
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getStringList('comparison_ids'), contains('A|1000'));

      // Create new provider to test load
      final newProvider = ContextReportProvider(
        apiService: apiService,
        historyService: _MockHistoryService(),
      );

      // Wait for loadComparisonSet
      await Future.delayed(const Duration(milliseconds: 50));

      expect(newProvider.comparisonIds, contains('A|1000'));
    });

    test('loadComparisonSet handles null or empty prefs gracefully', () async {
      final prefs = await SharedPreferences.getInstance();
      await prefs.clear(); // Ensure empty

      final newProvider = ContextReportProvider(
        apiService: apiService,
        historyService: _MockHistoryService(),
      );

      await Future.delayed(const Duration(milliseconds: 50));
      expect(newProvider.comparisonIds, isEmpty);
    });

    test('fetchReport handles errors gracefully', () async {
      apiService.shouldFail = true;
      // Accessing private method via public trigger
      await provider.addToComparison('ErrorQuery', 1000);

      // Should not throw and report should be missing
      expect(provider.getReportById('ErrorQuery|1000'), isNull);
    });
  });
}
