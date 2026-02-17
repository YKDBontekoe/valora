import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/formatters/currency_formatter.dart';
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
      'Ownership': listing.ownershipType,
      'Cadastral': listing.cadastralDesignation,
      'VvE Contribution': listing.vveContribution != null
          ? '${CurrencyFormatter.formatEur(listing.vveContribution!)} / month'
          : null,
      'Orientation': listing.gardenOrientation,
      'Energy Label': listing.energyLabel,
      'Fiber': listing.fiberAvailable == null
          ? null
          : (listing.fiberAvailable! ? 'Available' : 'Unavailable'),
      'WOZ Value': listing.wozValue != null
          ? CurrencyFormatter.formatEur(listing.wozValue!.toDouble())
          : null,
      'WOZ Reference': listing.wozReferenceDate != null
          ? '${listing.wozReferenceDate!.year}'
          : null,
      'WOZ Source': listing.wozValueSource,
      'Boiler': listing.cvBoilerBrand != null
          ? '${listing.cvBoilerBrand} (${listing.cvBoilerYear ?? "Unknown"})'
          : null,
      'Volume': listing.volumeM3 != null ? '${listing.volumeM3} m³' : null,
    };

    final validDetails = details.entries.where((e) => e.value != null).toList();

    if (validDetails.isEmpty) return const SizedBox.shrink();

    return Column(
      mainAxisSize: MainAxisSize.min,
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
          spacing: ValoraSpacing.sm,
          runSpacing: ValoraSpacing.sm,
          children: validDetails
              .map(
                (e) => ValoraGlassContainer(
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                  padding: const EdgeInsets.symmetric(
                    horizontal: ValoraSpacing.md,
                    vertical: ValoraSpacing.sm + 2,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        e.key,
                        style: ValoraTypography.labelSmall.copyWith(
                          color: colorScheme.onSurfaceVariant,
                          letterSpacing: 0.5,
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
