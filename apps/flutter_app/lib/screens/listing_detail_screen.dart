import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:url_launcher/url_launcher.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_animations.dart';
import '../models/listing.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_glass_container.dart';
import '../models/context_report.dart';
import '../widgets/report/context_report_view.dart';
import '../widgets/listing_detail/listing_sliver_app_bar.dart';
import '../widgets/listing_detail/listing_header.dart';
import '../widgets/listing_detail/listing_address.dart';
import '../widgets/listing_detail/listing_specs.dart';
import '../widgets/listing_detail/listing_features.dart';
import '../widgets/listing_detail/listing_technical_details.dart';
import '../widgets/listing_detail/listing_broker_card.dart';

class ListingDetailScreen extends StatelessWidget {
  const ListingDetailScreen({super.key, required this.listing});

  final Listing listing;

  Future<void> _openExternalLink(BuildContext context) async {
    final url = listing.url;
    if (url != null) {
      await _openUrl(context, url);
    }
  }

  Future<void> _contactBroker(BuildContext context) async {
    final phone = listing.brokerPhone;
    if (phone != null) {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (context) => ValoraDialog(
          title: 'Call Broker?',
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
            'Do you want to call ${listing.agentName ?? 'the broker'} at $phone?',
          ),
        ),
      );

      if (confirmed != true) return;

      final uri = Uri.parse('tel:${phone.replaceAll(RegExp(r'[^0-9+]'), '')}');
      try {
        if (!await launchUrl(uri)) {
          if (context.mounted) {
            _showErrorSnackBar(context, 'Could not launch dialer');
          }
        }
      } catch (e) {
        if (context.mounted) {
          _showErrorSnackBar(context, 'Error launching dialer: $e');
        }
      }
    }
  }

  void _showErrorSnackBar(BuildContext context, String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: ValoraColors.error),
    );
  }

  Future<void> _openMap(BuildContext context) async {
    final Uri uri;
    if (listing.latitude != null && listing.longitude != null) {
      uri = Uri.parse(
        'https://www.google.com/maps/search/?api=1&query=${listing.latitude},${listing.longitude}',
      );
    } else {
      final String query = '${listing.address} ${listing.city ?? ''}'.trim();
      uri = Uri.parse(
        'https://www.google.com/maps/search/?api=1&query=${Uri.encodeComponent(query)}',
      );
    }

    await _openUrl(context, uri.toString());
  }

  Future<void> _openVirtualTour(BuildContext context) async {
    if (listing.virtualTourUrl != null) {
      await _openUrl(context, listing.virtualTourUrl!);
    }
  }

  Future<void> _openVideo(BuildContext context) async {
    if (listing.videoUrl != null) {
      await _openUrl(context, listing.videoUrl!);
    }
  }

  Future<void> _openFirstFloorPlan(BuildContext context) async {
    if (listing.floorPlanUrls.isNotEmpty) {
      await _openUrl(context, listing.floorPlanUrls.first);
    }
  }

  Future<void> _openUrl(BuildContext context, String url) async {
    final Uri uri = Uri.parse(url);
    try {
      if (!await launchUrl(uri, mode: LaunchMode.externalApplication) &&
          context.mounted) {
        _showErrorSnackBar(context, 'Could not open $url');
      }
    } catch (e) {
      if (context.mounted) {
        _showErrorSnackBar(context, 'Error launching URL: $e');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final contextReport = listing.contextReport != null
        ? ContextReport.fromJson(listing.contextReport!)
        : null;

    return Scaffold(
      extendBodyBehindAppBar: true,
      body: CustomScrollView(
        physics: const BouncingScrollPhysics(),
        slivers: [
          ListingSliverAppBar(listing: listing),
          SliverToBoxAdapter(
            child: ValoraGlassContainer(
              margin: const EdgeInsets.all(ValoraSpacing.md),
              padding: const EdgeInsets.all(ValoraSpacing.lg),
              borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children:
                    [
                          ListingHeader(listing: listing),
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
                              'About this home',
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

                          // Key Features Grid
                          ListingFeatures(listing: listing),
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
                                    onPressed: () => _openMap(context),
                                  ),
                                if (listing.virtualTourUrl != null)
                                  ValoraButton(
                                    label: 'Virtual Tour',
                                    icon: Icons.view_in_ar_rounded,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () => _openVirtualTour(context),
                                  ),
                                if (listing.videoUrl != null)
                                  ValoraButton(
                                    label: 'Watch Video',
                                    icon: Icons.play_circle_outline_rounded,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () => _openVideo(context),
                                  ),
                                if (listing.floorPlanUrls.isNotEmpty)
                                  ValoraButton(
                                    label: 'Floorplan',
                                    icon: Icons.map_outlined,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () =>
                                        _openFirstFloorPlan(context),
                                  ),
                              ],
                            ),
                            const SizedBox(height: ValoraSpacing.xl),
                          ],

                          // Features List
                          if (listing.features.isNotEmpty) ...[
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
                                          style: ValoraTypography.bodyMedium
                                              .copyWith(
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
                              onPressed: () => _openExternalLink(context),
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
