import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/widgets/report/smart_insights_grid.dart';

void main() {
  final testLocation = ContextLocation(
    query: 'Test',
    displayAddress: 'Test Addr',
    latitude: 0,
    longitude: 0,
  );

  testWidgets('SmartInsightsGrid renders correct scores and labels', (
    tester,
  ) async {
    final report = ContextReport(
      location: testLocation,
      socialMetrics: [
        ContextMetric(key: 'average_woz', label: 'WOZ', source: 'S', score: 85),
      ],
      crimeMetrics: [],
      demographicsMetrics: [
        ContextMetric(
          key: 'family_friendly',
          label: 'Family',
          source: 'S',
          score: 90,
        ),
        ContextMetric(
          key: 'income_per_inhabitant',
          label: 'Income',
          source: 'S',
          score: 75,
        ),
      ],
      housingMetrics: [],
      mobilityMetrics: [],
      amenityMetrics: [],
      environmentMetrics: [],
      compositeScore: 80,
      categoryScores: {'Safety': 82, 'Amenities': 70},
      sources: [],
      warnings: [],
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: SmartInsightsGrid(report: report)),
      ),
    );
    await tester.pump();

    expect(find.text('Family Friendly'), findsOneWidget);
    expect(find.text('90%'), findsOneWidget);
    expect(find.text('Excellent for kids'), findsOneWidget);

    expect(find.text('Safety Level'), findsOneWidget);
    expect(find.text('82%'), findsOneWidget);
    expect(find.text('Very safe area'), findsOneWidget);

    expect(find.text('Connectivity'), findsOneWidget);
    expect(find.text('70%'), findsOneWidget);
    expect(find.text('Well connected'), findsOneWidget);

    expect(find.text('Economic Class'), findsOneWidget);
    expect(find.text('80%'), findsOneWidget); // (75 + 85) / 2
    expect(find.text('Upper middle class'), findsOneWidget);
  });

  testWidgets('SmartInsightsGrid handles partial fallbacks', (tester) async {
    final partialReport = ContextReport(
      location: testLocation,
      socialMetrics: [],
      crimeMetrics: [],
      demographicsMetrics: [
        ContextMetric(
          key: 'income_per_inhabitant',
          label: 'Inc',
          source: 'S',
          score: 90,
        ),
        // No family metrics at all
      ],
      housingMetrics: [],
      mobilityMetrics: [],
      amenityMetrics: [],
      environmentMetrics: [],
      compositeScore: 0,
      categoryScores: {},
      sources: [],
      warnings: [],
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: SmartInsightsGrid(report: partialReport)),
      ),
    );
    await tester.pump();

    // Economic: (90 + 50) / 2 = 70
    expect(find.text('70%'), findsOneWidget);
    expect(find.text('N/A'), findsNWidgets(3)); // Others are missing
  });

  testWidgets('SmartInsightsGrid handles missing data', (tester) async {
    final emptyReport = ContextReport(
      location: testLocation,
      socialMetrics: [],
      crimeMetrics: [],
      demographicsMetrics: [],
      housingMetrics: [],
      mobilityMetrics: [],
      amenityMetrics: [],
      environmentMetrics: [],
      compositeScore: 0,
      categoryScores: {},
      sources: [],
      warnings: [],
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: SmartInsightsGrid(report: emptyReport)),
      ),
    );
    await tester.pump();

    expect(find.text('N/A'), findsNWidgets(4));
    expect(find.text('Data unavailable'), findsNWidgets(4));
  });
}
