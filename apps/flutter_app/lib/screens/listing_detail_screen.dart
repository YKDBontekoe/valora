import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import 'dart:developer' as developer;

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

class ListingDetailScreen extends StatefulWidget {
  const ListingDetailScreen({super.key, required this.listing});

  final Listing listing;

  @override
  State<ListingDetailScreen> createState() => _ListingDetailScreenState();
}

class _ListingDetailScreenState extends State<ListingDetailScreen> {
  late Listing _listing;
  bool _isLoading = false;
  bool _fetchInitiated = false;

  @override
  void initState() {
    super.initState();
    _listing = widget.listing;
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_fetchInitiated) {
      _fetchInitiated = true;
      _fetchFullDetailsIfNeeded();
    }
  }

  Future<void> _fetchFullDetailsIfNeeded() async {
    // If we already have a description and photos, we might not need to fetch.
    // However, summary might miss details like description, features, etc.
    final bool isSummary = _listing.description == null &&
        _listing.features.isEmpty &&
        _listing.url != null;

    // Also check if we need photo enrichment
    final bool needsPhotos = _listing.imageUrls.isEmpty &&
                             (_listing.imageUrl == null || _listing.imageUrl!.isEmpty) &&
                             (_listing.latitude != null && _listing.longitude != null);

    // If it's a full listing and has photos, we are good.
    if (!isSummary && !needsPhotos) {
      return;
    }

    if (!mounted) return;

    setState(() {
      _isLoading = true;
    });

    final apiService = context.read<ApiService>();
    final photoService = context.read<PropertyPhotoService>();

    try {
      Listing updatedListing = _listing;

      // 1. Fetch full details if needed
      if (isSummary) {
         try {
           updatedListing = await apiService.getListing(_listing.id);
         } catch (e, stack) {
            developer.log('Failed to fetch full listing details', error: e, stackTrace: stack);
            // Continue with what we have
         }
      }

      // 2. Enrich with photos if needed
      // Check again with updated listing (which might now have lat/long)
      if (updatedListing.imageUrls.isEmpty &&
          (updatedListing.imageUrl == null || updatedListing.imageUrl!.isEmpty) &&
          updatedListing.latitude != null &&
          updatedListing.longitude != null) {

        try {
          final photoUrls = photoService.getPropertyPhotos(
            latitude: updatedListing.latitude!,
            longitude: updatedListing.longitude!,
          );

          if (photoUrls.isNotEmpty) {
             final serialized = updatedListing.toJson();
             serialized['imageUrl'] = photoUrls.first;
             serialized['imageUrls'] = photoUrls;
             updatedListing = Listing.fromJson(serialized);
          }
        } catch (e, stack) {
           developer.log('Failed to enrich listing photos', error: e, stackTrace: stack);
        }
      }

      if (mounted) {
        setState(() {
          _listing = updatedListing;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
        ScaffoldMessenger.maybeOf(context)?.showSnackBar(
          const SnackBar(
            content: Text('Could not load full listing details'),
          ),
        );
      }
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
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children:
                    [
                          if (_isLoading)
                            const Padding(
                              padding: EdgeInsets.only(bottom: ValoraSpacing.lg),
                              child: LinearProgressIndicator(),
                            ),

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
                             // Skeleton for description
                             const ValoraShimmer(width: double.infinity, height: 100),
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
