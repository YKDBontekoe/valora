import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/screens/context_report/widgets/compare_button.dart';
import 'package:valora_app/models/context_report.dart';

class MockContextReportProvider extends Mock implements ContextReportProvider {
  @override
  ContextReport? get report => super.noSuchMethod(
        Invocation.getter(#report),
        returnValue: null,
      );

  @override
  int get radiusMeters => 1000;

  @override
  bool isComparing(String? query, int? radius) => super.noSuchMethod(
        Invocation.method(#isComparing, [query, radius]),
        returnValue: false,
      );

  @override
  Future<void> toggleComparison(String? query, int? radius) => super.noSuchMethod(
        Invocation.method(#toggleComparison, [query, radius]),
        returnValue: Future.value(),
        returnValueForMissingStub: Future.value(),
      );
}

void main() {
  testWidgets('CompareButton toggles comparison state', (WidgetTester tester) async {
    final mockProvider = MockContextReportProvider();

    final mockReport = ContextReport(
      location: ContextLocation(
        query: 'Amsterdam',
        displayAddress: 'Amsterdam',
        municipalityName: 'Amsterdam',
        neighborhoodName: 'Centrum',
        latitude: 52.3676,
        longitude: 4.9041,
      ),
      compositeScore: 80,
      categoryScores: {},
      socialMetrics: [],
      crimeMetrics: [],
      demographicsMetrics: [],
      housingMetrics: [],
      mobilityMetrics: [],
      amenityMetrics: [],
      environmentMetrics: [],
      sources: [],
      warnings: [],
    );

    when(mockProvider.report).thenReturn(mockReport);
    when(mockProvider.isComparing(any, any)).thenReturn(false);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: CompareButton(provider: mockProvider),
        ),
      ),
    );

    // Initial state: not comparing
    expect(find.byIcon(Icons.playlist_add_rounded), findsOneWidget);

    // Tap button
    await tester.tap(find.byType(InkWell));
    await tester.pump();

    verify(mockProvider.toggleComparison('Amsterdam', 1000)).called(1);

    // Change state to comparing
    when(mockProvider.isComparing(any, any)).thenReturn(true);
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: CompareButton(provider: mockProvider),
        ),
      ),
    );

    // Verify icon changed
    expect(find.byIcon(Icons.playlist_add_check_rounded), findsOneWidget);
  });
}
