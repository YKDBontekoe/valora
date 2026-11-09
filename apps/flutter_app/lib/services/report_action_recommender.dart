import 'package:flutter/material.dart';

import '../models/context_report.dart';
import '../models/report_action.dart';

class ReportActionRecommender {
  static List<ReportAction> recommend(
    ContextReport report, {
    required bool isComparing,
    required bool isSaved,
    required bool hasAiInsight,
    required Set<String> dismissedIds,
    required Set<String> completedIds,
  }) {
    final List<ReportAction> actions = [];

    // 1. Comparison Action
    if (!isComparing) {
      const id = 'action_compare';
      if (!_isHandled(id, dismissedIds, completedIds)) {
        actions.add(
          const ReportAction(
            id: id,
            title: 'Compare',
            description: 'Compare with other neighborhoods',
            icon: Icons.compare_arrows_rounded,
            type: ActionType.comparison,
          ),
        );
      }
    }

    // 2. Save Action
    if (!isSaved) {
      const id = 'action_save';
      if (!_isHandled(id, dismissedIds, completedIds)) {
        actions.add(
          const ReportAction(
            id: id,
            title: 'Save Search',
            description: 'Get updates for this location',
            icon: Icons.bookmark_border_rounded,
            type: ActionType.save,
          ),
        );
      }
    }

    // 3. Map/Amenities Action
    // Recommend if there are amenity metrics to show
    if (report.amenityMetrics.isNotEmpty) {
      const id = 'action_map';
      if (!_isHandled(id, dismissedIds, completedIds)) {
        actions.add(
          const ReportAction(
            id: id,
            title: 'View Map',
            description: 'Explore amenities nearby',
            icon: Icons.map_rounded,
            type: ActionType.map,
          ),
        );
      }
    }

    // 4. AI Insight Action
    if (!hasAiInsight) {
      const id = 'action_ai';
      if (!_isHandled(id, dismissedIds, completedIds)) {
        actions.add(
          const ReportAction(
            id: id,
            title: 'AI Analysis',
            description: 'Get deep insights',
            icon: Icons.psychology_rounded,
            type: ActionType.ai,
          ),
        );
      }
    }

    return actions;
  }

  static bool _isHandled(
    String id,
    Set<String> dismissedIds,
    Set<String> completedIds,
  ) {
    return dismissedIds.contains(id) || completedIds.contains(id);
  }
}
