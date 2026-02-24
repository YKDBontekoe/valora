import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/models/report_action.dart';
import 'package:valora_app/services/report_action_recommender.dart';

void main() {
  group('ReportActionRecommender', () {
    final emptyReport = ContextReport(
      location: ContextLocation(
        query: 'test',
        displayAddress: 'Test Address',
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
      compositeScore: 0,
      categoryScores: {},
      sources: [],
      warnings: [],
    );

    test('recommends valid actions when state is clean', () {
      final actions = ReportActionRecommender.recommend(
        emptyReport,
        isComparing: false,
        isSaved: false,
        hasAiInsight: false,
        dismissedIds: {},
        completedIds: {},
      );

      final types = actions.map((a) => a.type).toList();
      expect(types, contains(ActionType.comparison));
      expect(types, contains(ActionType.save));
      expect(types, contains(ActionType.ai));
      expect(types, isNot(contains(ActionType.map))); // No amenities
    });

    test('recommends map action if amenities exist', () {
      final reportWithAmenities = ContextReport(
        location: emptyReport.location,
        socialMetrics: [],
        crimeMetrics: [],
        demographicsMetrics: [],
        housingMetrics: [],
        mobilityMetrics: [],
        amenityMetrics: [
          ContextMetric(
            key: 'park',
            label: 'Park',
            source: 'Test',
            value: 1,
          )
        ],
        environmentMetrics: [],
        compositeScore: 0,
        categoryScores: {},
        sources: [],
        warnings: [],
      );

      final actions = ReportActionRecommender.recommend(
        reportWithAmenities,
        isComparing: false,
        isSaved: false,
        hasAiInsight: false,
        dismissedIds: {},
        completedIds: {},
      );

      expect(actions.any((a) => a.type == ActionType.map), isTrue);
    });

    test('filters out handled actions', () {
      final actions = ReportActionRecommender.recommend(
        emptyReport,
        isComparing: false,
        isSaved: false,
        hasAiInsight: false,
        dismissedIds: {'action_compare'},
        completedIds: {'action_save'},
      );

      final types = actions.map((a) => a.type).toList();
      expect(types, isNot(contains(ActionType.comparison)));
      expect(types, isNot(contains(ActionType.save)));
      expect(types, contains(ActionType.ai));
    });

    test('filters out unnecessary actions based on state', () {
      final actions = ReportActionRecommender.recommend(
        emptyReport,
        isComparing: true,
        isSaved: true,
        hasAiInsight: true,
        dismissedIds: {},
        completedIds: {},
      );

      expect(actions, isEmpty);
    });
  });
}
