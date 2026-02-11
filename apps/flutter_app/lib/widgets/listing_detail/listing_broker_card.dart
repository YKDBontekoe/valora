import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';

class ListingBrokerCard extends StatelessWidget {
  const ListingBrokerCard({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    if (listing.agentName == null && listing.brokerLogoUrl == null && listing.brokerPhone == null) {
      return const SizedBox.shrink();
    }

    return Container(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      decoration: BoxDecoration(
        color: colorScheme.primaryContainer.withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        border: Border.all(color: colorScheme.primary.withValues(alpha: 0.2)),
      ),
      child: Row(
        children: [
          if (listing.brokerLogoUrl != null)
            Container(
              width: 50,
              height: 50,
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                image: DecorationImage(
                  image: NetworkImage(listing.brokerLogoUrl!),
                  fit: BoxFit.contain,
                ),
              ),
            )
          else
            Container(
              width: 50,
              height: 50,
              decoration: BoxDecoration(
                color: colorScheme.primary,
                borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
              ),
              child: const Icon(Icons.business, color: Colors.white),
            ),
          const SizedBox(width: ValoraSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Broker',
                  style: ValoraTypography.labelMedium.copyWith(
                    color: colorScheme.primary,
                  ),
                ),
                Text(
                  listing.agentName ?? 'Real Estate Agent',
                  style: ValoraTypography.titleMedium.copyWith(
                    color: colorScheme.onSurface,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                if (listing.brokerPhone != null)
                  Text(
                    listing.brokerPhone!,
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
