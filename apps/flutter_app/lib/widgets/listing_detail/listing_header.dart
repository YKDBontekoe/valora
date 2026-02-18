import 'package:flutter/material.dart';
import '../../core/utils/listing_utils.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';
import '../valora_widgets.dart';

class ListingHeader extends StatelessWidget {
  const ListingHeader({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: listing.price != null
              ? ValoraPrice(price: listing.price!, size: ValoraPriceSize.large)
              : Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Property Report',
                      style: ValoraTypography.headlineMedium.copyWith(
                        color: colorScheme.primary,
                      ),
                    ),
                    Text(
                      'Analyzed via Valora AI',
                      style: ValoraTypography.labelSmall.copyWith(
                        color: colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
        ),
        if (listing.status != null && listing.status != 'Unknown') ...[
          const SizedBox(width: ValoraSpacing.md),
          ValoraBadge(
            label: listing.status!.toUpperCase(),
            color: ListingUtils.getStatusColor(listing.status!),
          ),
        ],
      ],
    );
  }
}
