import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/saved_listing.dart';
import '../../providers/workspace_provider.dart';
import '../../screens/saved_listing_detail_screen.dart';
import '../common/valora_card.dart';
import '../common/valora_shimmer.dart';

class SavedListingItem extends StatelessWidget {
  final SavedListing savedListing;

  const SavedListingItem({super.key, required this.savedListing});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final listing = savedListing.listing;
    final city = listing?.city;

    return ValoraCard(
      onTap: () {
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => ChangeNotifierProvider.value(
              value: context.read<WorkspaceProvider>(),
              child: SavedListingDetailScreen(savedListing: savedListing),
            ),
          ),
        );
      },
      padding: const EdgeInsets.all(ValoraSpacing.md),
      child: Row(
        children: [
          // Thumbnail
          Container(
            width: 64,
            height: 64,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(12),
              color: ValoraColors.primary.withValues(alpha: 0.08),
            ),
            clipBehavior: Clip.antiAlias,
            child: listing?.imageUrl != null
                ? CachedNetworkImage(
                    imageUrl: listing!.imageUrl!,
                    fit: BoxFit.cover,
                    placeholder: (context, url) => const ValoraShimmer(
                      width: double.infinity,
                      height: double.infinity,
                    ),
                    errorWidget: (context, url, error) => const Center(
                      child: Icon(Icons.home_rounded,
                          color: ValoraColors.primary, size: 28),
                    ),
                  )
                : const Center(
                    child: Icon(Icons.home_rounded,
                        color: ValoraColors.primary, size: 28),
                  ),
          ),
          const SizedBox(width: ValoraSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  listing?.address ?? 'Unknown Address',
                  style: ValoraTypography.titleSmall.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (city != null && city.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(
                    city,
                    style: ValoraTypography.bodySmall.copyWith(
                      color: isDark
                          ? ValoraColors.neutral400
                          : ValoraColors.neutral500,
                    ),
                  ),
                ],
                const SizedBox(height: 6),
                Row(
                  children: [
                    Icon(Icons.chat_bubble_outline_rounded,
                        size: 14,
                        color: isDark
                            ? ValoraColors.neutral500
                            : ValoraColors.neutral400),
                    const SizedBox(width: 4),
                    Text(
                      savedListing.commentCount == 1
                          ? '1 comment'
                          : '${savedListing.commentCount} comments',
                      style: ValoraTypography.labelSmall.copyWith(
                        color: isDark
                            ? ValoraColors.neutral500
                            : ValoraColors.neutral400,
                      ),
                    ),
                    if (savedListing.notes != null &&
                        savedListing.notes!.isNotEmpty) ...[
                      const SizedBox(width: 12),
                      Icon(Icons.note_rounded,
                          size: 14,
                          color: isDark
                              ? ValoraColors.neutral500
                              : ValoraColors.neutral400),
                      const SizedBox(width: 4),
                      Flexible(
                        child: Text(
                          savedListing.notes!,
                          style: ValoraTypography.labelSmall.copyWith(
                            color: isDark
                                ? ValoraColors.neutral500
                                : ValoraColors.neutral400,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ],
                ),
              ],
            ),
          ),
          Icon(Icons.chevron_right_rounded,
              color: isDark
                  ? ValoraColors.neutral500
                  : ValoraColors.neutral400),
        ],
      ),
    );
  }
}
