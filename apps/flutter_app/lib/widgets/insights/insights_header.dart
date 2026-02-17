import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../providers/insights_provider.dart';

class InsightsHeader extends StatelessWidget {
  const InsightsHeader({super.key});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Positioned(
      top: 12,
      left: 12,
      right: 12,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
          boxShadow: ValoraShadows.md,
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          child: Row(
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  gradient: ValoraColors.primaryGradient,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(
                  Icons.insights_rounded,
                  color: Colors.white,
                  size: 20,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Area Insights',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                        color: Theme.of(context).textTheme.titleMedium?.color,
                      ),
                    ),
                    Selector<InsightsProvider, int>(
                      selector: (_, p) => p.cities.length,
                      builder: (context, count, _) {
                        return Text(
                          '$count cities',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: Theme.of(context).textTheme.bodySmall?.color,
                          ),
                        );
                      },
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
