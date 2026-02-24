import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import '../../models/saved_listing.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../common/valora_card.dart';
import '../common/valora_price.dart';
import '../common/valora_shimmer.dart';

class ValoraListingCard extends StatelessWidget {
  const ValoraListingCard({
    super.key,
    required this.listing,
    this.onTap,
    this.commentCount,
    this.notes,
  });

  final ListingSummary listing;
  final VoidCallback? onTap;
  final int? commentCount;
  final String? notes;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return ValoraCard(
      onTap: onTap,
      padding: EdgeInsets.zero,
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Image Section
          SizedBox(
            height: 200,
            child: Stack(
              fit: StackFit.expand,
              children: [
                if (listing.imageUrl != null)
                  CachedNetworkImage(
                    imageUrl: listing.imageUrl!,
                    fit: BoxFit.cover,
                    placeholder: (context, url) => const ValoraShimmer(
                      width: double.infinity,
                      height: double.infinity,
                    ),
                    errorWidget: (context, url, error) => Container(
                      color: isDark ? ValoraColors.neutral800 : ValoraColors.neutral100,
                      child: Icon(
                        Icons.image_not_supported_rounded,
                        color: isDark ? ValoraColors.neutral600 : ValoraColors.neutral400,
                      ),
                    ),
                  )
                else
                  Container(
                    color: isDark ? ValoraColors.neutral800 : ValoraColors.neutral100,
                    child: Icon(
                      Icons.home_rounded,
                      size: 48,
                      color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral300,
                    ),
                  ),

                // Price Tag Overlay
                if (listing.price != null)
                  Positioned(
                    bottom: ValoraSpacing.md,
                    left: ValoraSpacing.md,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: ValoraSpacing.sm,
                        vertical: ValoraSpacing.xs,
                      ),
                      decoration: BoxDecoration(
                        color: ValoraColors.surfaceDark.withValues(alpha: 0.9),
                        borderRadius: BorderRadius.circular(ValoraSpacing.radiusSm),
                      ),
                      child: ValoraPrice(
                        price: listing.price!,
                        size: ValoraPriceSize.small,
                        color: Colors.white,
                      ),
                    ),
                  ),
              ],
            ),
          ),

          // Details Section
          Padding(
            padding: const EdgeInsets.all(ValoraSpacing.md),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  listing.address,
                  style: ValoraTypography.titleMedium.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (listing.city != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    listing.city!,
                    style: ValoraTypography.bodySmall.copyWith(
                      color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                    ),
                  ),
                ],

                const SizedBox(height: ValoraSpacing.md),

                // Specs Row
                Row(
                  children: [
                    if (listing.bedrooms != null) ...[
                      _SpecItem(icon: Icons.bed_rounded, label: '${listing.bedrooms} bed'),
                      const SizedBox(width: ValoraSpacing.md),
                    ],
                    if (listing.livingAreaM2 != null) ...[
                      _SpecItem(icon: Icons.square_foot_rounded, label: '${listing.livingAreaM2} m²'),
                    ],
                  ],
                ),

                // Footer with comments/notes if present
                if ((commentCount != null && commentCount! > 0) || (notes != null && notes!.isNotEmpty)) ...[
                  const SizedBox(height: ValoraSpacing.md),
                  Divider(height: 1, color: isDark ? ValoraColors.neutral800 : ValoraColors.neutral200),
                  const SizedBox(height: ValoraSpacing.sm),
                  Row(
                    children: [
                      if (commentCount != null && commentCount! > 0) ...[
                        Icon(Icons.chat_bubble_outline_rounded,
                          size: 14,
                          color: isDark ? ValoraColors.primaryLight : ValoraColors.primary,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '$commentCount',
                          style: ValoraTypography.labelSmall.copyWith(
                            color: isDark ? ValoraColors.primaryLight : ValoraColors.primary,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(width: ValoraSpacing.md),
                      ],
                      if (notes != null && notes!.isNotEmpty)
                        Expanded(
                          child: Row(
                            children: [
                              Icon(Icons.note_rounded,
                                size: 14,
                                color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                              ),
                              const SizedBox(width: 4),
                              Expanded(
                                child: Text(
                                  notes!,
                                  style: ValoraTypography.labelSmall.copyWith(
                                    color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _SpecItem extends StatelessWidget {
  const _SpecItem({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(
          icon,
          size: 16,
          color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
        ),
        const SizedBox(width: 4),
        Text(
          label,
          style: ValoraTypography.bodySmall.copyWith(
            color: isDark ? ValoraColors.neutral300 : ValoraColors.neutral600,
            fontWeight: FontWeight.w500,
          ),
        ),
      ],
    );
  }
}
