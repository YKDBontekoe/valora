import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';

class ListingFeatures extends StatelessWidget {
  const ListingFeatures({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final features = <Widget>[];

    if (listing.energyLabel != null) {
      features.add(
        _buildFeatureChip(
          Icons.energy_savings_leaf_rounded,
          'Label ${listing.energyLabel}',
          colorScheme,
        ),
      );
    }
    if (listing.yearBuilt != null) {
      features.add(
        _buildFeatureChip(
          Icons.calendar_today_rounded,
          'Built ${listing.yearBuilt}',
          colorScheme,
        ),
      );
    }
    if (listing.ownershipType != null) {
      features.add(
        _buildFeatureChip(
          Icons.gavel_rounded,
          listing.ownershipType!,
          colorScheme,
        ),
      );
    }
    if (listing.heatingType != null) {
      features.add(
        _buildFeatureChip(
          Icons.thermostat_rounded,
          listing.heatingType!,
          colorScheme,
        ),
      );
    }
    if (listing.hasGarage) {
      features.add(
        _buildFeatureChip(Icons.garage_rounded, 'Garage', colorScheme),
      );
    }

    if (features.isEmpty) return const SizedBox.shrink();

    return Wrap(
      spacing: ValoraSpacing.sm,
      runSpacing: ValoraSpacing.sm,
      children: features,
    );
  }

  Widget _buildFeatureChip(
    IconData icon,
    String label,
    ColorScheme colorScheme,
  ) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: ValoraSpacing.md - 4,
        vertical: ValoraSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: colorScheme.secondaryContainer.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.3),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            icon,
            size: ValoraSpacing.iconSizeSm,
            color: colorScheme.onSecondaryContainer,
          ),
          const SizedBox(width: ValoraSpacing.sm),
          Text(
            label,
            style: ValoraTypography.labelLarge.copyWith(
              color: colorScheme.onSecondaryContainer,
            ),
          ),
        ],
      ),
    );
  }
}
