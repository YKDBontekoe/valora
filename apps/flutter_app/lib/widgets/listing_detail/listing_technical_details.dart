import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';
import '../valora_glass_container.dart';

class ListingTechnicalDetails extends StatelessWidget {
  const ListingTechnicalDetails({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final details = <String, String?>{
      'Roof Type': listing.roofType,
      'Construction': listing.constructionPeriod,
      'Insulation': listing.insulationType,
      'Parking': listing.parkingType,
      'Orientation': listing.gardenOrientation,
      'Boiler': listing.cvBoilerBrand != null
          ? '${listing.cvBoilerBrand} (${listing.cvBoilerYear ?? "Unknown"})'
          : null,
      'Volume': listing.volumeM3 != null ? '${listing.volumeM3} m³' : null,
    };

    final validDetails = details.entries.where((e) => e.value != null).toList();

    if (validDetails.isEmpty) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Details',
          style: ValoraTypography.titleLarge.copyWith(
            color: colorScheme.onSurface,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: ValoraSpacing.md),
        Wrap(
          spacing: ValoraSpacing.md,
          runSpacing: ValoraSpacing.md,
          children: validDetails
              .map(
                (e) => ValoraGlassContainer(
                  padding: const EdgeInsets.symmetric(
                    horizontal: ValoraSpacing.md,
                    vertical: ValoraSpacing.sm,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        e.key,
                        style: ValoraTypography.labelSmall.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        e.value!,
                        style: ValoraTypography.bodyMedium.copyWith(
                          color: colorScheme.onSurface,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              )
              .toList(),
        ),
      ],
    );
  }
}
