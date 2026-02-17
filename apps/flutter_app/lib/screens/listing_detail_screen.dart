import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_animations.dart';
import '../core/utils/listing_url_launcher.dart';
import '../models/listing.dart';
import '../services/api_service.dart';
import '../services/property_photo_service.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_glass_container.dart';
import '../models/context_report.dart';
import '../widgets/report/context_report_view.dart';
import '../widgets/listing_detail/listing_sliver_app_bar.dart';
import '../widgets/listing_detail/listing_header.dart';
import '../widgets/listing_detail/listing_address.dart';
import '../widgets/listing_detail/listing_specs.dart';
import '../widgets/listing_detail/listing_highlights.dart';
import '../widgets/listing_detail/listing_detailed_features.dart';
import '../widgets/listing_detail/listing_technical_details.dart';
import '../widgets/listing_detail/listing_broker_card.dart';
import 'gallery/full_screen_gallery.dart';
import 'dart:developer' as developer;

class ListingDetailScreen extends StatefulWidget {
  const ListingDetailScreen({super.key, required this.listing});

  final Listing listing;

  @override
  State<ListingDetailScreen> createState() => _ListingDetailScreenState();
}

class _ListingDetailScreenState extends State<ListingDetailScreen> {
  late Listing _listing;
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _listing = widget.listing;

    // Check if we need to fetch full details
    // If the listing is a summary (missing description or features), fetch full details
    // Only do this if we have a URL, which indicates a DB-backed listing (PDOK lookups lack URLs)
    bool needsFetch = _listing.description == null &&
        _listing.features.isEmpty &&
        _listing.url != null;

    if (needsFetch) {
      _isLoading = true;
    }

    // Defer the async work to avoid setState during build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _enrichListing();
    });
  }

  Future<void> _enrichListing() async {
    if (_isLoading) {
      try {
        final fullListing = await context.read<ApiService>().getListing(
          _listing.id,
        );
        if (mounted) {
          setState(() {
            _listing = fullListing;
          });
        }
      } catch (e, stack) {
        developer.log(
          'Failed to fetch full listing details',
          name: 'ListingDetailScreen',
          error: e,
          stackTrace: stack,
        );
        // Continue with partial data
      } finally {
        if (mounted) {
          setState(() => _isLoading = false);
        }
      }
    }

    // 2. Enrich with real photos if needed
    // This is now done here instead of blocking the navigation
    await _enrichWithPhotos();
  }

  Future<void> _enrichWithPhotos() async {
    final hasPhotos =
        _listing.imageUrls.isNotEmpty ||
        (_listing.imageUrl?.trim().isNotEmpty ?? false);

    if (hasPhotos || _listing.latitude == null || _listing.longitude == null) {
      return;
    }

    // Move the photo enrichment logic here
    try {
      final photoService = context.read<PropertyPhotoService>();
      final photoUrls = photoService.getPropertyPhotos(
        latitude: _listing.latitude!,
        longitude: _listing.longitude!,
      );

      if (photoUrls.isNotEmpty && mounted) {
        final serialized = _listing.toJson();
        serialized['imageUrl'] = photoUrls.first;
        serialized['imageUrls'] = photoUrls;
        setState(() {
          _listing = Listing.fromJson(serialized);
        });
      }
    } catch (e) {
      // Ignore photo errors
    }
  }

  Future<void> _contactBroker(BuildContext context) async {
    final phone = _listing.brokerPhone;
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
            'Do you want to call ${_listing.agentName ?? 'the broker'} at $phone?',
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
    final contextReport = _listing.contextReport != null
        ? ContextReport.fromJson(_listing.contextReport!)
        : null;

    // Use imageUrls if available, otherwise fallback to single imageUrl, otherwise empty list
    final images = _listing.imageUrls.isNotEmpty
        ? _listing.imageUrls
        : (_listing.imageUrl != null ? [_listing.imageUrl!] : <String>[]);

    return Scaffold(
      extendBodyBehindAppBar: true,
      body: CustomScrollView(
        physics: const BouncingScrollPhysics(),
        slivers: [
          ListingSliverAppBar(
            listing: _listing,
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
                crossAxisAlignment: CrossAxisAlignment.start,
                children:
                    [
                          ListingHeader(listing: _listing),
                          const SizedBox(height: ValoraSpacing.lg),
                          ListingAddress(listing: _listing),
                          const SizedBox(height: ValoraSpacing.lg),
                          Divider(color: colorScheme.outlineVariant),
                          const SizedBox(height: ValoraSpacing.lg),
                          ListingSpecs(listing: _listing),
                          const SizedBox(height: ValoraSpacing.xl),

                          // Description
                          if (_listing.description != null) ...[
                            Text(
                              'About this home',
                              style: ValoraTypography.titleLarge.copyWith(
                                color: colorScheme.onSurface,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(height: ValoraSpacing.sm),
                            Text(
                              _listing.description!,
                              style: ValoraTypography.bodyMedium.copyWith(
                                color: colorScheme.onSurfaceVariant,
                                height: 1.5,
                              ),
                            ),
                            const SizedBox(height: ValoraSpacing.xl),
                          ] else if (_isLoading) ...[
                             // Loading skeleton for description
                             const ValoraShimmer(width: 150, height: 24),
                             const SizedBox(height: ValoraSpacing.sm),
                             const ValoraShimmer(width: double.infinity, height: 16),
                             const SizedBox(height: 8),
                             const ValoraShimmer(width: double.infinity, height: 16),
                             const SizedBox(height: 8),
                             const ValoraShimmer(width: 200, height: 16),
                             const SizedBox(height: ValoraSpacing.xl),
                          ],

                          // Key Features Grid (Highlights)
                          ListingHighlights(listing: _listing),
                          const SizedBox(height: ValoraSpacing.xl),

                          // Technical Details
                          ListingTechnicalDetails(listing: _listing),
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

                          if (_listing.virtualTourUrl != null ||
                              _listing.videoUrl != null ||
                              _listing.floorPlanUrls.isNotEmpty ||
                              _listing.latitude != null ||
                              _listing.longitude != null) ...[
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
                                if (_listing.latitude != null ||
                                    _listing.longitude != null)
                                  ValoraButton(
                                    label: 'Open Map',
                                    icon: Icons.map_rounded,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () => ListingUrlLauncher.openMap(context, _listing.latitude, _listing.longitude, _listing.address, _listing.city),
                                  ),
                                if (_listing.virtualTourUrl != null)
                                  ValoraButton(
                                    label: 'Virtual Tour',
                                    icon: Icons.view_in_ar_rounded,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () => ListingUrlLauncher.openVirtualTour(context, _listing.virtualTourUrl),
                                  ),
                                if (_listing.videoUrl != null)
                                  ValoraButton(
                                    label: 'Watch Video',
                                    icon: Icons.play_circle_outline_rounded,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () => ListingUrlLauncher.openVideo(context, _listing.videoUrl),
                                  ),
                                if (_listing.floorPlanUrls.isNotEmpty)
                                  ValoraButton(
                                    label: 'Floorplan',
                                    icon: Icons.map_outlined,
                                    variant: ValoraButtonVariant.secondary,
                                    onPressed: () =>
                                        ListingUrlLauncher.openFirstFloorPlan(context, _listing.floorPlanUrls),
                                  ),
                              ],
                            ),
                            const SizedBox(height: ValoraSpacing.xl),
                          ],

                          // Features List (Detailed)
                          ListingDetailedFeatures(listing: _listing),

                          // Broker Section
                          if (_listing.brokerLogoUrl != null ||
                              _listing.brokerPhone != null) ...[
                            ListingBrokerCard(listing: _listing),
                            const SizedBox(height: ValoraSpacing.md),
                          ],

                          if (_listing.url != null) ...[
                            const SizedBox(height: ValoraSpacing.sm),
                            ValoraButton(
                              label: 'View on Funda',
                              icon: Icons.open_in_new_rounded,
                              isFullWidth: true,
                              onPressed: () => ListingUrlLauncher.openExternalLink(context, _listing.url),
                            ),
                          ],

                          if (_listing.brokerPhone != null) ...[
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
