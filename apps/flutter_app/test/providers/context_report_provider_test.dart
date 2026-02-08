import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/services/api_service.dart';

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
      safetyMetrics: <ContextMetric>[],
      amenityMetrics: <ContextMetric>[],
      environmentMetrics: <ContextMetric>[],
      compositeScore: 88.2,
      sources: <SourceAttribution>[],
      warnings: <String>[],
    );
  }

  test('generate stores report on success', () async {
    final provider = ContextReportProvider(apiService: _FakeApiService(report: buildReport()));

    await provider.generate('Damrak 1 Amsterdam');

    expect(provider.error, isNull);
    expect(provider.report, isNotNull);
    expect(provider.report!.compositeScore, 88.2);
    expect(provider.isLoading, isFalse);
  });

  test('generate sets user-friendly error on failure', () async {
    final provider = ContextReportProvider(
      apiService: _FakeApiService(error: ServerException('boom')),
    );

    await provider.generate('Damrak 1 Amsterdam');

    expect(provider.report, isNull);
    expect(provider.error, 'boom');
    expect(provider.isLoading, isFalse);
  });

  test('generate requires input', () async {
    final provider = ContextReportProvider(apiService: _FakeApiService(report: buildReport()));

    await provider.generate('   ');

    expect(provider.error, 'Enter an address or listing link.');
    expect(provider.report, isNull);
  });
}
