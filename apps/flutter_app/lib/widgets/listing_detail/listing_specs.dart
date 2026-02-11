import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';

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
            .map(
              (widget) => Padding(
                padding: const EdgeInsets.only(right: ValoraSpacing.lg),
                child: widget,
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
    return Container(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.5),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(ValoraSpacing.sm),
            decoration: BoxDecoration(
              color: colorScheme.surface,
              shape: BoxShape.circle,
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
