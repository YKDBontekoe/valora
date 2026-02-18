import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_animations.dart';
import '../core/utils/listing_url_launcher.dart';
import '../models/listing.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_glass_container.dart';
import '../models/context_report.dart';
import '../widgets/report/context_report_view.dart';
import '../widgets/listing_detail/listing_sliver_app_bar.dart';
import '../widgets/listing_detail/listing_header.dart';
import '../widgets/listing_detail/market_intelligence_card.dart';
import '../widgets/listing_detail/listing_address.dart';
import '../widgets/listing_detail/listing_specs.dart';
import '../widgets/listing_detail/listing_highlights.dart';
import '../widgets/listing_detail/listing_detailed_features.dart';
import '../widgets/listing_detail/listing_technical_details.dart';
import '../widgets/listing_detail/listing_broker_card.dart';
import 'gallery/full_screen_gallery.dart';

class ListingDetailScreen extends StatelessWidget {
  const ListingDetailScreen({super.key, required this.listing});

  final Listing listing;

  Future<void> _contactBroker(BuildContext context) async {
    final phone = listing.brokerPhone;
    if (phone != null) {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (context) => ValoraDialog(
          title: 'Contact Broker',
          actions: [
            ValoraButton(
              label: 'Cancel',
              variant: ValoraButtonVariant.ghost,
              onPressed: () => Navigator.pop(context, false),
            ),
            ValoraButton(
              label: 'Call',
              variant: ValoraButtonVariant.primary,
              onPressed: () => Navigator.pop(context, true),
            ),
          ],
          child: Text(
            'Do you want to call ${listing.agentName ?? "the broker"} at $phone?',
          ),
        ),
      );

      if (confirmed == true && context.mounted) {
        await ListingUrlLauncher.contactBroker(context, phone);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final contextReport = listing.contextReport != null
        ? ContextReport.fromJson(listing.contextReport!)
        : null;

    // Use imageUrls if available, otherwise fallback to single imageUrl, otherwise empty list
    final images = listing.imageUrls.isNotEmpty
        ? listing.imageUrls
        : (listing.imageUrl != null ? [listing.imageUrl!] : <String>[]);

    return Scaffold(
      extendBodyBehindAppBar: true,
      body: CustomScrollView(
        physics: const BouncingScrollPhysics(),
        slivers: [
          ListingSliverAppBar(
            listing: listing,
            onImageTap: (index) {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => FullScreenGallery(
                    imageUrls: images,
                    initialIndex: index,
                  ),
                ),
              );
            },
          ),
          SliverToBoxAdapter(
            child: ValoraGlassContainer(
              margin: const EdgeInsets.all(ValoraSpacing.md),
              padding: const EdgeInsets.all(ValoraSpacing.lg),
              borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                          ListingHeader(listing: listing),
                          const SizedBox(height: ValoraSpacing.md),
                          MarketIntelligenceCard(listing: listing),
                          const SizedBox(height: ValoraSpacing.lg),
                          ListingAddress(listing: listing),
                          const SizedBox(height: ValoraSpacing.lg),
                          Divider(color: colorScheme.outlineVariant),
                          const SizedBox(height: ValoraSpacing.lg),
                          ListingSpecs(listing: listing),
                          const SizedBox(height: ValoraSpacing.xl),

                          // Description
                          if (listing.description != null) ...[
                            Text(
                              'Property Intelligence Analysis',
                              style: ValoraTypography.titleLarge.copyWith(
                                color: colorScheme.onSurface,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(height: ValoraSpacing.sm),
                            Text(
                              listing.description!,
                              style: ValoraTypography.bodyMedium.copyWith(
                                color: colorScheme.onSurfaceVariant,
                                height: 1.5,
                              ),
                            ),
                            const SizedBox(height: ValoraSpacing.xl),
                          ],

                          // Key Features Grid (Highlights)
                          ListingHighlights(listing: listing),
                          const SizedBox(height: ValoraSpacing.xl),

                          // Technical Details
                          ListingTechnicalDetails(listing: listing),
                          const SizedBox(height: ValoraSpacing.xl),

                          if (contextReport != null) ...[
                             Text(
                              'Neighborhood Analytics',
                              style: ValoraTypography.titleLarge.copyWith(
                                color: colorScheme.onSurface,
                                fontWeight: FontWeight.bold,
                              ),
                             ),
                             const SizedBox(height: ValoraSpacing.md),
                             ContextReportView(
                               report: contextReport,
                               showHeader: false,
                             ),
                             const SizedBox(height: ValoraSpacing.xl),
                          ],

                          if (listing.virtualTourUrl != null ||
                              listing.videoUrl != null ||
                              listing.floorPlanUrls.isNotEmpty ||
                              listing.latitude != null ||
                              listing.longitude != null) ...[
                            Text(
                              'Explore',
                              style: ValoraTypography.titleLarge.copyWith(
                                color: colorScheme.onSurface,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(height: ValoraSpacing.md),
                            Wrap(
                              spacing: ValoraSpacing.sm,
                              runSpacing: ValoraSpacing.sm,
                              children: [
                                if (listing.latitude != null ||
                                    listing.longitude != null)
                                  ValoraButton(
                                    label: 'Open Map',
                                    icon: Icons.map_rounded,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () => ListingUrlLauncher.openMap(context, listing.latitude, listing.longitude, listing.address, listing.city),
                                  ),
                                if (listing.virtualTourUrl != null)
                                  ValoraButton(
                                    label: 'Virtual Tour',
                                    icon: Icons.view_in_ar_rounded,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () => ListingUrlLauncher.openVirtualTour(context, listing.virtualTourUrl),
                                  ),
                                if (listing.videoUrl != null)
                                  ValoraButton(
                                    label: 'Watch Video',
                                    icon: Icons.play_circle_outline_rounded,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () => ListingUrlLauncher.openVideo(context, listing.videoUrl),
                                  ),
                                if (listing.floorPlanUrls.isNotEmpty)
                                  ValoraButton(
                                    label: 'Floorplan',
                                    icon: Icons.map_outlined,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () =>
                                        ListingUrlLauncher.openFirstFloorPlan(context, listing.floorPlanUrls),
                                  ),
                              ],
                            ),
                            const SizedBox(height: ValoraSpacing.xl),
                          ],

                          // Features List (Detailed)
                          ListingDetailedFeatures(listing: listing),

                          // Broker Section
                          if (listing.brokerLogoUrl != null ||
                              listing.brokerPhone != null) ...[
                            ListingBrokerCard(listing: listing),
                            const SizedBox(height: ValoraSpacing.md),
                          ],

                          if (listing.url != null) ...[
                            const SizedBox(height: ValoraSpacing.sm),
                            ValoraButton(
                              label: 'View on Funda',
                              icon: Icons.open_in_new_rounded,
                              isFullWidth: true,
                              onPressed: () => ListingUrlLauncher.openExternalLink(context, listing.url),
                            ),
                          ],

                          if (listing.brokerPhone != null) ...[
                            const SizedBox(height: ValoraSpacing.md),
                            ValoraButton(
                              label: 'Contact Broker',
                              icon: Icons.phone_rounded,
                              variant: ValoraButtonVariant.outline,
                              isFullWidth: true,
                              onPressed: () => _contactBroker(context),
                            ),
                          ],

                          const SizedBox(height: ValoraSpacing.xl),
                        ]
                        .animate(interval: 50.ms)
                        .fade(duration: ValoraAnimations.slow)
                        .slideY(
                          begin: 0.1,
                          end: 0,
                          curve: ValoraAnimations.deceleration,
                        ),
              ),
            ),
          ),
          // Add extra padding at bottom to ensure scrollability
          const SliverToBoxAdapter(child: SizedBox(height: ValoraSpacing.xxl)),
        ],
      ),
    );
  }
}
