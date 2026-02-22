import 'package:flutter/material.dart';
import '../valora_widgets.dart';

class ContextReportSkeleton extends StatelessWidget {
  const ContextReportSkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        // Header skeleton
        const ValoraShimmer(height: 100, borderRadius: 24),
        const SizedBox(height: 24),

        // Gauge/Radar skeleton
        Row(
          children: [
            const Expanded(child: ValoraShimmer(height: 140, borderRadius: 70)),
            const SizedBox(width: 24),
            const Expanded(child: ValoraShimmer(height: 140, borderRadius: 20)),
          ],
        ),
        const SizedBox(height: 32),

        // Smart Insights skeleton
        const ValoraShimmer(width: 150, height: 20, borderRadius: 4),
        const SizedBox(height: 16),
        Column(
          children: [
            Row(
              children: [
                const Expanded(
                  child: AspectRatio(
                    aspectRatio: 1.4,
                    child: ValoraShimmer(borderRadius: 20),
                  ),
                ),
                const SizedBox(width: 16),
                const Expanded(
                  child: AspectRatio(
                    aspectRatio: 1.4,
                    child: ValoraShimmer(borderRadius: 20),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                const Expanded(
                  child: AspectRatio(
                    aspectRatio: 1.4,
                    child: ValoraShimmer(borderRadius: 20),
                  ),
                ),
                const SizedBox(width: 16),
                const Expanded(
                  child: AspectRatio(
                    aspectRatio: 1.4,
                    child: ValoraShimmer(borderRadius: 20),
                  ),
                ),
              ],
            ),
          ],
        ),
        const SizedBox(height: 32),

        // AI Insight skeleton
        const ValoraShimmer(height: 180, borderRadius: 20),
        const SizedBox(height: 32),

        // Categories skeleton
        ...List.generate(
          3,
          (index) => const Padding(
            padding: EdgeInsets.only(bottom: 16),
            child: ValoraShimmer(height: 80, borderRadius: 20),
          ),
        ),
      ],
    );
  }
}
