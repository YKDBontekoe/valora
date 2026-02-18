import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/utils/listing_utils.dart';
import '../../models/listing.dart';
import '../valora_widgets.dart';
import 'package:intl/intl.dart';

class MarketIntelligenceCard extends StatelessWidget {
  const MarketIntelligenceCard({super.key, required this.listing});

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final hasScore = listing.contextCompositeScore != null;
    final hasWoz = listing.wozValue != null;

    if (!hasScore && !hasWoz) return const SizedBox.shrink();

    return Container(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.surfaceVariantDark : ValoraColors.neutral100,
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.5),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                Icons.insights_rounded,
                size: 20,
                color: colorScheme.primary,
              ),
              const SizedBox(width: ValoraSpacing.xs),
              Text(
                'Market Intelligence',
                style: ValoraTypography.labelLarge.copyWith(
                  color: colorScheme.primary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          const SizedBox(height: ValoraSpacing.md),
          Row(
            children: [
              if (hasScore)
                Expanded(
                  child: _MetricItem(
                    label: 'Valora Score',
                    value: listing.contextCompositeScore!.toStringAsFixed(1),
                    color: ListingUtils.getScoreColor(listing.contextCompositeScore!),
                    icon: Icons.star_rounded,
                  ),
                ),
              if (hasScore && hasWoz)
                Container(
                  height: 40,
                  width: 1,
                  color: colorScheme.outlineVariant,
                  margin: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md),
                ),
              if (hasWoz)
                Expanded(
                  child: _MetricItem(
                    label: 'Estimated Value',
                    value: NumberFormat.compactCurrency(
                      symbol: '€',
                      locale: 'nl_NL',
                    ).format(listing.wozValue),
                    color: colorScheme.secondary,
                    icon: Icons.euro_symbol_rounded,
                    subtitle: listing.wozReferenceDate != null
                        ? 'WOZ ${listing.wozReferenceDate!.year}'
                        : 'Market Estimate',
                  ),
                ),
            ],
          ),
          if (hasScore) ...[
            const SizedBox(height: ValoraSpacing.md),
            _buildComparisonBar(context),
          ],
        ],
      ),
    );
  }

  Widget _buildComparisonBar(BuildContext context) {
    final score = listing.contextCompositeScore ?? 0;
    final color = ListingUtils.getScoreColor(score);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              'Neighborhood Percentile',
              style: ValoraTypography.labelSmall.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
            Text(
              '${(score * 10).toInt()}th',
              style: ValoraTypography.labelSmall.copyWith(
                color: color,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        const SizedBox(height: ValoraSpacing.xs),
        ClipRRect(
          borderRadius: BorderRadius.circular(2),
          child: LinearProgressIndicator(
            value: score / 10,
            backgroundColor: color.withValues(alpha: 0.1),
            valueColor: AlwaysStoppedAnimation<Color>(color),
            minHeight: 4,
          ),
        ),
      ],
    );
  }
}

class _MetricItem extends StatelessWidget {
  const _MetricItem({
    required this.label,
    required this.value,
    required this.color,
    required this.icon,
    this.subtitle,
  });

  final String label;
  final String value;
  final Color color;
  final IconData icon;
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: ValoraTypography.labelSmall.copyWith(
            color: colorScheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: ValoraSpacing.xs),
        Row(
          children: [
            Icon(icon, size: 16, color: color),
            const SizedBox(width: ValoraSpacing.xs),
            Text(
              value,
              style: ValoraTypography.titleLarge.copyWith(
                color: colorScheme.onSurface,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        if (subtitle != null)
          Text(
            subtitle!,
            style: ValoraTypography.labelSmall.copyWith(
              color: colorScheme.onSurfaceVariant.withValues(alpha: 0.7),
              fontSize: 10,
            ),
          ),
      ],
    );
  }
}
