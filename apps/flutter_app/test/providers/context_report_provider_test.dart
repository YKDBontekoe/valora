import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/models/search_history_item.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/search_history_service.dart';

class _FakeApiService extends ApiService {
  _FakeApiService({this.report, this.error});

  final ContextReport? report;
  final Exception? error;

  @override
  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    if (error != null) {
      throw error!;
    }

    return report!;
  }
}

class _FailingHistoryService extends SearchHistoryService {
  @override
  Future<void> addToHistory(String query) async {
    throw Exception('History failed');
  }
}

void main() {
  ContextReport buildReport() {
    return ContextReport(
      location: ContextLocation(
        query: 'Damrak 1 Amsterdam',
        displayAddress: 'Damrak 1, 1012LG Amsterdam',
        latitude: 52.3771,
        longitude: 4.8980,
      ),
      socialMetrics: <ContextMetric>[],
      crimeMetrics: <ContextMetric>[],
      demographicsMetrics: <ContextMetric>[],
      housingMetrics: <ContextMetric>[],
      mobilityMetrics: <ContextMetric>[],
      amenityMetrics: <ContextMetric>[],
      environmentMetrics: <ContextMetric>[],
      compositeScore: 88.2,
      categoryScores: <String, double>{
        'Social': 85.0,
        'Safety': 90.0,
        'Demographics': 80.0,
        'Amenities': 92.0,
        'Environment': 88.0,
      },
      sources: <SourceAttribution>[],
      warnings: <String>[],
    );
  }

  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  test('generate stores report and updates history on success', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(report: buildReport()),
      historyService: SearchHistoryService(),
    );

    await provider.generate('Damrak 1 Amsterdam');

    expect(provider.error, isNull);
    expect(provider.report, isNotNull);
    expect(provider.report!.compositeScore, 88.2);
    expect(provider.isLoading, isFalse);

    // Check history
    expect(provider.history.length, 1);
    expect(provider.history.first.query, 'Damrak 1 Amsterdam');
  });

  test('generate sets user-friendly error on failure and does not update history', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(error: ServerException('boom')),
      historyService: SearchHistoryService(),
    );

    await provider.generate('Damrak 1 Amsterdam');

    expect(provider.report, isNull);
    expect(provider.error, 'boom');
    expect(provider.isLoading, isFalse);

    // Check history
    expect(provider.history, isEmpty);
  });

  test('generate returns report even if history service fails', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(report: buildReport()),
      historyService: _FailingHistoryService(),
    );

    await provider.generate('Damrak 1 Amsterdam');

    expect(provider.error, isNull);
    expect(provider.report, isNotNull);
    expect(provider.isLoading, isFalse);
    // History should be empty or unchanged due to failure, but report succeeds
  });

  test('generate requires input', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(report: buildReport()),
      historyService: SearchHistoryService(),
    );

    await provider.generate('   ');

    expect(provider.error, 'Enter an address or location link.');
    expect(provider.report, isNull);
  });

  test('history operations work', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(report: buildReport()),
      historyService: SearchHistoryService(),
    );

    // Seed history via generate
    await provider.generate('search 1');
    expect(provider.history.length, 1);

    // Test remove
    await provider.removeFromHistory('search 1');
    expect(provider.history, isEmpty);

    // Seed again
    await provider.generate('search 2');
    expect(provider.history.length, 1);

    // Test clear
    await provider.clearHistory();
    expect(provider.history, isEmpty);
  });

  test('history list is unmodifiable', () async {
     final provider = ContextReportProvider(
      apiService: _FakeApiService(report: buildReport()),
      historyService: SearchHistoryService(),
    );

    await provider.generate('search 1');

    expect(() => provider.history.add(SearchHistoryItem(query: 'test', timestamp: DateTime.now())), throwsUnsupportedError);
  });
}
