import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';

class ListingDetailedFeatures extends StatelessWidget {
  const ListingDetailedFeatures({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    if (listing.features.isEmpty) return const SizedBox.shrink();

    final colorScheme = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Features',
          style: ValoraTypography.titleLarge.copyWith(
            color: colorScheme.onSurface,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: ValoraSpacing.md),
        ...listing.features.entries.map(
          (e) => Padding(
            padding: const EdgeInsets.only(
              bottom: ValoraSpacing.sm,
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  Icons.check_circle_outline_rounded,
                  size: 20,
                  color: colorScheme.primary,
                ),
                const SizedBox(width: ValoraSpacing.sm),
                Expanded(
                  child: RichText(
                    text: TextSpan(
                      style: ValoraTypography.bodyMedium.copyWith(
                        color: colorScheme.onSurface,
                      ),
                      children: [
                        TextSpan(
                          text: '${e.key}: ',
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        TextSpan(text: e.value),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: ValoraSpacing.xl),
      ],
    );
  }
}
