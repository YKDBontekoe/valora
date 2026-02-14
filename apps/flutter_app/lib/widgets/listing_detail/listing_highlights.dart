import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../models/listing.dart';
import '../valora_widgets.dart';

class ListingHighlights extends StatelessWidget {
  const ListingHighlights({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final features = <Widget>[];

    if (listing.energyLabel != null) {
      features.add(
        ValoraTag(
          icon: Icons.energy_savings_leaf_rounded,
          label: 'Label ${listing.energyLabel}',
        ),
      );
    }
    if (listing.yearBuilt != null) {
      features.add(
        ValoraTag(
          icon: Icons.calendar_today_rounded,
          label: 'Built ${listing.yearBuilt}',
        ),
      );
    }
    if (listing.ownershipType != null) {
      features.add(
        ValoraTag(
          icon: Icons.gavel_rounded,
          label: listing.ownershipType!,
        ),
      );
    }
    if (listing.heatingType != null) {
      features.add(
        ValoraTag(
          icon: Icons.thermostat_rounded,
          label: listing.heatingType!,
        ),
      );
    }
    if (listing.hasGarage) {
      features.add(
        const ValoraTag(
          icon: Icons.garage_rounded,
          label: 'Garage',
        ),
      );
    }
    if (listing.fiberAvailable != null) {
      features.add(
        ValoraTag(
          icon: Icons.wifi_rounded,
          label: listing.fiberAvailable! ? 'Fiber Available' : 'No Fiber',
        ),
      );
    }
    if (listing.contextCompositeScore != null) {
      features.add(
        ValoraTag(
          icon: Icons.analytics_rounded,
          label: 'Valora ${listing.contextCompositeScore!.toStringAsFixed(1)}',
        ),
      );
    }
    if (listing.contextSafetyScore != null) {
      features.add(
        ValoraTag(
          icon: Icons.shield_rounded,
          label: 'Safety ${listing.contextSafetyScore!.toStringAsFixed(1)}',
        ),
      );
    }

    if (features.isEmpty) return const SizedBox.shrink();

    return Wrap(
      spacing: ValoraSpacing.sm,
      runSpacing: ValoraSpacing.sm,
      children: features,
    );
  }
}
