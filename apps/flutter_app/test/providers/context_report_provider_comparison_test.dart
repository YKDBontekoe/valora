import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/repositories/context_report_repository.dart';
import 'package:valora_app/repositories/ai_repository.dart';
import 'package:valora_app/services/search_history_service.dart';

import 'context_report_provider_comparison_test.mocks.dart';

@GenerateMocks([ContextReportRepository, AiRepository, SearchHistoryService])
void main() {
  late ContextReportProvider provider;
  late MockContextReportRepository mockRepo;
  late MockAiRepository mockAiRepo;
  late MockSearchHistoryService mockHistory;

  setUp(() {
    mockRepo = MockContextReportRepository();
    mockAiRepo = MockAiRepository();
    mockHistory = MockSearchHistoryService();

    // Default history behavior
    when(mockHistory.getHistory()).thenAnswer((_) async => []);

    provider = ContextReportProvider(
      contextReportRepository: mockRepo,
      aiRepository: mockAiRepo,
      historyService: mockHistory,
    );
  });

  ContextReport createReport(String query) {
    return ContextReport(
      location: ContextLocation(
        query: query,
        displayAddress: '$query Address',
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
      compositeScore: 80,
      categoryScores: {},
      sources: [],
      warnings: [],
    );
  }

  test('addToComparison adds ID and fetches report if missing', () async {
    when(mockRepo.getContextReport(any, radiusMeters: anyNamed('radiusMeters')))
        .thenAnswer((_) async => createReport('LocA'));

    await provider.addToComparison('LocA', 1000);

    expect(provider.comparisonIds.length, 1);
    verify(mockRepo.getContextReport('LocA', radiusMeters: 1000)).called(1);
  });

  test('toggleComparison removes if exists', () async {
    when(mockRepo.getContextReport(any, radiusMeters: anyNamed('radiusMeters')))
        .thenAnswer((_) async => createReport('LocA'));

    await provider.addToComparison('LocA', 1000);
    expect(provider.comparisonIds.length, 1);

    await provider.toggleComparison('LocA', 1000);
    expect(provider.comparisonIds.isEmpty, true);
  });

  test('clearComparison clears all', () async {
    when(mockRepo.getContextReport(any, radiusMeters: anyNamed('radiusMeters')))
        .thenAnswer((_) async => createReport('LocA'));

    await provider.addToComparison('LocA', 1000);
    await provider.addToComparison('LocB', 1000);

    expect(provider.comparisonIds.length, 2);

    provider.clearComparison();
    expect(provider.comparisonIds.isEmpty, true);
  });
}
