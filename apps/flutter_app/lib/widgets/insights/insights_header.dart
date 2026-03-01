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
    return DecoratedBox(
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
        boxShadow: isDark ? ValoraShadows.mdDark : ValoraShadows.md,
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 11),
        child: Row(
          children: [
            // Icon with gradient background
            Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                gradient: ValoraColors.primaryGradient,
                borderRadius: BorderRadius.circular(11),
                boxShadow: [
                  BoxShadow(
                    color: ValoraColors.primary.withValues(alpha: 0.35),
                    blurRadius: 10,
                    offset: const Offset(0, 3),
                  ),
                ],
              ),
              child: const Icon(Icons.insights_rounded, color: Colors.white, size: 20),
            ),
            const SizedBox(width: 10),
            // Title + subtitle
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    'Area Insights',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                      letterSpacing: -0.2,
                    ),
                  ),
                  Selector<InsightsProvider, (int, bool)>(
                    selector: (_, p) => (p.cities.length, p.isLoading),
                    builder: (context, data, _) {
                      final count = data.$1;
                      final isLoading = data.$2;
                      return Row(
                        children: [
                          if (isLoading) ...[
                            SizedBox(
                              width: 10,
                              height: 10,
                              child: CircularProgressIndicator(
                                strokeWidth: 1.5,
                                valueColor: AlwaysStoppedAnimation(
                                  Theme.of(context).textTheme.bodySmall?.color,
                                ),
                              ),
                            ),
                            const SizedBox(width: 6),
                            Text(
                              'Loading…',
                              style: Theme.of(context).textTheme.bodySmall?.copyWith(fontSize: 11.5),
                            ),
                          ] else ...[
                            Container(
                              width: 6,
                              height: 6,
                              decoration: const BoxDecoration(
                                color: ValoraColors.success,
                                shape: BoxShape.circle,
                              ),
                            ),
                            const SizedBox(width: 5),
                            Text(
                              '$count ${count == 1 ? 'city' : 'cities'} in view',
                              style: Theme.of(context).textTheme.bodySmall?.copyWith(fontSize: 11.5),
                            ),
                          ],
                        ],
                      );
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

}
