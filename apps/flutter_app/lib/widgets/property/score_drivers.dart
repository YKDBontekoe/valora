import 'package:flutter/material.dart';
import '../../models/property_detail.dart';
import '../report/score_gauge.dart';

class ScoreDrivers extends StatelessWidget {
  final PropertyDetail property;

  const ScoreDrivers({super.key, required this.property});

  @override
  Widget build(BuildContext context) {
    if (property.contextCompositeScore == null) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Context Scores',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _buildScore(context, 'Composite', property.contextCompositeScore),
              _buildScore(context, 'Safety', property.contextSafetyScore),
              _buildScore(context, 'Social', property.contextSocialScore),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildScore(BuildContext context, String label, double? score) {
    if (score == null) return const SizedBox.shrink();
    return Column(
      children: [
        ScoreGauge(score: score, size: 60),
        const SizedBox(height: 8),
        Text(label, style: Theme.of(context).textTheme.bodyMedium),
      ],
    );
  }
}
