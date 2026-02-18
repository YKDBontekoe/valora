import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import '../../models/listing.dart';
import '../valora_widgets.dart';

class ListingSpecs extends StatelessWidget {
  const ListingSpecs({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final specs = <Widget>[];

    if (listing.bedrooms != null) {
      specs.add(
        _buildSpecItem(
          Icons.bed_rounded,
          'Bedrooms',
          '${listing.bedrooms}',
          colorScheme,
        ),
      );
    }

    if (listing.bathrooms != null) {
      specs.add(
        _buildSpecItem(
          Icons.shower_rounded,
          'Bathrooms',
          '${listing.bathrooms}',
          colorScheme,
        ),
      );
    }

    if (listing.livingAreaM2 != null) {
      specs.add(
        _buildSpecItem(
          Icons.square_foot_rounded,
          'Living Area',
          '${listing.livingAreaM2} m²',
          colorScheme,
        ),
      );
    }

    if (listing.plotAreaM2 != null) {
      specs.add(
        _buildSpecItem(
          Icons.landscape_rounded,
          'Plot Size',
          '${listing.plotAreaM2} m²',
          colorScheme,
        ),
      );
    }

    if (specs.isEmpty) return const SizedBox.shrink();

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      clipBehavior: Clip.none,
      child: Row(
        children: specs
            .asMap()
            .entries
            .map(
              (entry) => Padding(
                padding: const EdgeInsets.only(right: ValoraSpacing.lg),
                child: entry.value
                    .animate(delay: (100 * entry.key).ms)
                    .fade(duration: ValoraAnimations.normal)
                    .slideX(
                      begin: 0.2,
                      end: 0,
                      curve: ValoraAnimations.deceleration,
                    ),
              ),
            )
            .toList(),
      ),
    );
  }

  Widget _buildSpecItem(
    IconData icon,
    String label,
    String value,
    ColorScheme colorScheme,
  ) {
    return ValoraCard(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      backgroundColor: colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(ValoraSpacing.sm),
            decoration: BoxDecoration(
              color: colorScheme.surface,
              shape: BoxShape.circle,
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.05),
                  blurRadius: 4,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            child: Icon(icon, size: 20, color: colorScheme.primary),
          ),
          const SizedBox(width: ValoraSpacing.md),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                label,
                style: ValoraTypography.labelSmall.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: ValoraTypography.titleMedium.copyWith(
                  color: colorScheme.onSurface,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          const SizedBox(width: ValoraSpacing.sm),
        ],
      ),
    );
  }
}
