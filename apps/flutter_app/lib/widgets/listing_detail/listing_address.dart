import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';

class ListingAddress extends StatelessWidget {
  const ListingAddress({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          listing.address,
          style: ValoraTypography.headlineSmall.copyWith(
            color: colorScheme.onSurface,
          ),
        ),
        const SizedBox(height: ValoraSpacing.xs),
        Text(
          '${listing.city ?? ''} ${listing.postalCode ?? ''}'.trim(),
          style: ValoraTypography.bodyLarge.copyWith(
            color: colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}
