import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/search_history_service.dart';

class _FakeApiService extends ApiService {
  _FakeApiService({this.location, this.error});

  final ContextLocation? location;
  final Exception? error;

  @override
  Future<ContextLocation?> resolveLocation(String input) async {
    if (error != null) throw error!;
    return location;
  }

  @override
  Future<ContextCategoryMetrics> getContextMetrics(String category, ContextLocation location, {int? radiusMeters}) async {
    return ContextCategoryMetrics(metrics: [], warnings: [], score: 80.0);
  }
}

void main() {
  ContextLocation buildLocation() {
    return ContextLocation(
      query: 'Damrak 1 Amsterdam',
      displayAddress: 'Damrak 1, 1012LG Amsterdam',
      latitude: 52.3771,
      longitude: 4.8980,
    );
  }

  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  test('generate stores location and updates history on success', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(location: buildLocation()),
      historyService: SearchHistoryService(),
    );

    await provider.generate('Damrak 1 Amsterdam');

    expect(provider.error, isNull);
    expect(provider.location, isNotNull);
    expect(provider.isLoading, isFalse);

    expect(provider.history.length, 1);
    expect(provider.history.first.query, 'Damrak 1 Amsterdam');
  });

  test('generate sets user-friendly error on failure and does not update history', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(error: ServerException('boom')),
      historyService: SearchHistoryService(),
    );

    await provider.generate('Damrak 1 Amsterdam');

    expect(provider.location, isNull);
    expect(provider.error, 'boom');
    expect(provider.isLoading, isFalse);

    expect(provider.history, isEmpty);
  });

  test('generate requires input', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(location: buildLocation()),
      historyService: SearchHistoryService(),
    );

    await provider.generate('   ');

    expect(provider.error, 'Enter an address or listing link.');
    expect(provider.location, isNull);
  });
}
