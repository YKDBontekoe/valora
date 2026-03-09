import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/repositories/context_report_repository.dart';
import 'package:valora_app/services/api_client.dart';
import 'package:valora_app/models/context_report.dart';

@GenerateMocks([ApiClient])
import 'context_report_repository_test.mocks.dart';

void main() {
  late ContextReportRepository repository;
  late MockApiClient mockClient;

  setUp(() {
    mockClient = MockApiClient();
    repository = ContextReportRepository(mockClient);
  });

  group('ContextReportRepository', () {
    test('getContextReport parses response correctly', () async {
      final jsonBody = jsonEncode({
        'location': {
          'query': 'Test',
          'displayAddress': 'Address',
          'latitude': 52.0,
          'longitude': 4.0
        },
        'socialMetrics': [],
        'crimeMetrics': [],
        'demographicsMetrics': [],
        'housingMetrics': [],
        'mobilityMetrics': [],
        'amenityMetrics': [],
        'environmentMetrics': [],
        'compositeScore': 85.0,
        'categoryScores': {},
        'sources': [],
        'warnings': []
      });
      final response = http.Response(jsonBody, 200);

      when(mockClient.post(
        '/context/report',
        data: anyNamed('data'),
      )).thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.getContextReport('Test');

      expect(result.compositeScore, 85.0);
      expect(result.location.query, 'Test');
    });

    test('getAiAnalysis returns summary string', () async {
      final jsonBody = jsonEncode({'summary': 'Great area!'});
      final response = http.Response(jsonBody, 200);

      when(mockClient.post(
        '/ai/analyze-report',
        data: anyNamed('data'),
        timeout: anyNamed('timeout'),
      )).thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final report = ContextReport(
        location: ContextLocation(query: 'T', displayAddress: 'A', latitude: 0, longitude: 0),
        socialMetrics: [], crimeMetrics: [], demographicsMetrics: [], housingMetrics: [],
        mobilityMetrics: [], amenityMetrics: [], environmentMetrics: [],
        compositeScore: 0, categoryScores: {}, sources: [], warnings: []
      );

      final result = await repository.getAiAnalysis(report);

      expect(result, 'Great area!');
    });
  });
}
