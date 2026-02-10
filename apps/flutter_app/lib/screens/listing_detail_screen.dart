import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import 'package:share_plus/share_plus.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:url_launcher/url_launcher.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_animations.dart';
import '../models/listing.dart';
import '../providers/favorites_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_glass_container.dart';
import '../models/context_report.dart';
import '../widgets/report/context_report_view.dart';
import 'gallery/full_screen_gallery.dart';

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
            _showErrorSnackBar(context, 'Could not launch dialer. Please check your device settings.');
          }
        }
      } catch (e, stack) {
        debugPrintStack(label: 'Error launching dialer: $e', stackTrace: stack);
        if (context.mounted) {
          _showErrorSnackBar(context, 'Could not launch dialer. Please try again.');
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
        _showErrorSnackBar(context, 'Could not open link. Please check your browser settings.');
      }
    } catch (e, stack) {
      debugPrintStack(label: 'Error launching URL: $e', stackTrace: stack);
      if (context.mounted) {
        _showErrorSnackBar(context, 'Could not open link. Please try again.');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;
    final contextReport = listing.contextReport != null
        ? ContextReport.fromJson(listing.contextReport!)
        : null;

    return Scaffold(
      extendBodyBehindAppBar: true,
      body: CustomScrollView(
        physics: const BouncingScrollPhysics(),
        slivers: [
          _buildSliverAppBar(context, isDark),
          SliverToBoxAdapter(
            child: ValoraGlassContainer(
              margin: const EdgeInsets.all(ValoraSpacing.md),
              padding: const EdgeInsets.all(ValoraSpacing.lg),
              borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children:
                    [
                          _buildHeader(colorScheme),
                          const SizedBox(height: ValoraSpacing.lg),
                          _buildAddressSection(colorScheme),
                          const SizedBox(height: ValoraSpacing.lg),
                          Divider(color: colorScheme.outlineVariant),
                          const SizedBox(height: ValoraSpacing.lg),
                          _buildMainSpecsGrid(context, colorScheme),
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
                          _buildKeyFeaturesGrid(context, colorScheme),
                          const SizedBox(height: ValoraSpacing.xl),

                          // Technical Details
                          _buildTechnicalDetails(context, colorScheme),
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
                            _buildBrokerSection(colorScheme),
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

  Widget _buildSliverAppBar(BuildContext context, bool isDark) {
    // Use imageUrls if available, otherwise fallback to single imageUrl, otherwise empty list
    final images = listing.imageUrls.isNotEmpty
        ? listing.imageUrls
        : (listing.imageUrl != null ? [listing.imageUrl!] : <String>[]);

    return SliverAppBar(
      expandedHeight: 400,
      pinned: true,
      stretch: true,
      backgroundColor: Colors.transparent,
      iconTheme: const IconThemeData(color: Colors.white),
      actions: [
        if (listing.url != null)
          IconButton(
            // ignore: deprecated_member_use
            onPressed: () => Share.share(listing.url!),
            icon: const Icon(Icons.share_rounded, color: Colors.white),
          ),
        Consumer<FavoritesProvider>(
          builder: (context, favorites, _) {
            final isFav = favorites.isFavorite(listing.id);
            return IconButton(
              onPressed: () => favorites.toggleFavorite(listing),
              icon: Icon(
                isFav ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                color: isFav ? ValoraColors.error : Colors.white,
              ),
            );
          },
        ),
        const SizedBox(width: 8),
      ],
      flexibleSpace: FlexibleSpaceBar(
        background: Stack(
          fit: StackFit.expand,
          children: [
            if (images.isNotEmpty)
              PageView.builder(
                itemCount: images.length,
                itemBuilder: (context, index) {
                  return GestureDetector(
                    onTap: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => FullScreenGallery(
                            imageUrls: images,
                            initialIndex: index,
                          ),
                        ),
                      );
                    },
                    child: CachedNetworkImage(
                      imageUrl: images[index],
                      fit: BoxFit.cover,
                      placeholder: (context, url) =>
                          _buildPlaceholder(isDark, isLoading: true),
                      errorWidget: (context, url, error) =>
                          _buildPlaceholder(isDark),
                    ),
                  );
                },
              )
            else
              _buildPlaceholder(isDark),

            // Gradient overlay for text readability
            DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Colors.black.withValues(alpha: 0.6),
                    Colors.transparent,
                    Colors.transparent,
                    Colors.black.withValues(alpha: 0.7),
                  ],
                  stops: const [0.0, 0.3, 0.7, 1.0],
                ),
              ),
            ),

            // Photo Counter
            if (images.length > 1)
              Positioned(
                bottom:
                    ValoraSpacing.lg + 20, // Adjust for rounded corners of body
                right: ValoraSpacing.md,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.6),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(
                        Icons.photo_library_rounded,
                        size: 14,
                        color: Colors.white,
                      ),
                      const SizedBox(width: 6),
                      Text(
                        '${images.length} Photos',
                        style: ValoraTypography.labelMedium.copyWith(
                          color: Colors.white,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildPlaceholder(bool isDark, {bool isLoading = false}) {
    if (isLoading) {
      return const ValoraShimmer(
        width: double.infinity,
        height: double.infinity,
        borderRadius: 0,
      );
    }
    // If we have lat/long, we could show a static map here, but for now a nice icon
    // showing it's a "Building" record vs a "Sale" record
    return Container(
      color: isDark ? ValoraColors.surfaceVariantDark : ValoraColors.neutral100,
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.location_city_rounded,
              size: ValoraSpacing.iconSizeXl * 1.5,
              color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
            ),
             const SizedBox(height: ValoraSpacing.md),
             Text(
              'Property Details',
              style: ValoraTypography.titleMedium.copyWith(
                color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
              ),
             ),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(ColorScheme colorScheme) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: listing.price != null
              ? ValoraPrice(price: listing.price!, size: ValoraPriceSize.large)
              : Text(
                  'Check Report',
                  style: ValoraTypography.headlineMedium.copyWith(
                    color: colorScheme.primary,
                    fontWeight: FontWeight.bold,
                  ),
                ),
        ),
        if (listing.status != null) ...[
          const SizedBox(width: ValoraSpacing.md),
          ValoraBadge(
            label: listing.status!.toUpperCase(),
            color: _getStatusColor(listing.status!),
          ),
        ],
      ],
    );
  }

  Widget _buildAddressSection(ColorScheme colorScheme) {
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

  Widget _buildMainSpecsGrid(BuildContext context, ColorScheme colorScheme) {
    final specs = <Widget>[];

    if (listing.bedrooms != null) {
      specs.add(
        _buildSpecItem(
          Icons.bed_rounded,
          'Bedrooms',
          '${listing.bedrooms}',
          colorScheme,
        ),
      );
    }

    if (listing.bathrooms != null) {
      specs.add(
        _buildSpecItem(
          Icons.shower_rounded,
          'Bathrooms',
          '${listing.bathrooms}',
          colorScheme,
        ),
      );
    }

    if (listing.livingAreaM2 != null) {
      specs.add(
        _buildSpecItem(
          Icons.square_foot_rounded,
          'Living Area',
          '${listing.livingAreaM2} m²',
          colorScheme,
        ),
      );
    }

    if (listing.plotAreaM2 != null) {
      specs.add(
        _buildSpecItem(
          Icons.landscape_rounded,
          'Plot Size',
          '${listing.plotAreaM2} m²',
          colorScheme,
        ),
      );
    }

    if (specs.isEmpty) return const SizedBox.shrink();

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      clipBehavior: Clip.none,
      child: Row(
        children: specs
            .map(
              (widget) => Padding(
                padding: const EdgeInsets.only(right: ValoraSpacing.lg),
                child: widget,
              ),
            )
            .toList(),
      ),
    );
  }

  Widget _buildKeyFeaturesGrid(BuildContext context, ColorScheme colorScheme) {
    final features = <Widget>[];

    if (listing.energyLabel != null) {
      features.add(
        _buildFeatureChip(
          Icons.energy_savings_leaf_rounded,
          'Label ${listing.energyLabel}',
          colorScheme,
        ),
      );
    }
    if (listing.yearBuilt != null) {
      features.add(
        _buildFeatureChip(
          Icons.calendar_today_rounded,
          'Built ${listing.yearBuilt}',
          colorScheme,
        ),
      );
    }
    if (listing.ownershipType != null) {
      features.add(
        _buildFeatureChip(
          Icons.gavel_rounded,
          listing.ownershipType!,
          colorScheme,
        ),
      );
    }
    if (listing.heatingType != null) {
      features.add(
        _buildFeatureChip(
          Icons.thermostat_rounded,
          listing.heatingType!,
          colorScheme,
        ),
      );
    }
    if (listing.hasGarage) {
      features.add(
        _buildFeatureChip(Icons.garage_rounded, 'Garage', colorScheme),
      );
    }

    if (features.isEmpty) return const SizedBox.shrink();

    return Wrap(
      spacing: ValoraSpacing.sm,
      runSpacing: ValoraSpacing.sm,
      children: features,
    );
  }

  Widget _buildFeatureChip(
    IconData icon,
    String label,
    ColorScheme colorScheme,
  ) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: ValoraSpacing.md - 4,
        vertical: ValoraSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: colorScheme.secondaryContainer.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.3),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            icon,
            size: ValoraSpacing.iconSizeSm,
            color: colorScheme.onSecondaryContainer,
          ),
          const SizedBox(width: ValoraSpacing.sm),
          Text(
            label,
            style: ValoraTypography.labelLarge.copyWith(
              color: colorScheme.onSecondaryContainer,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTechnicalDetails(BuildContext context, ColorScheme colorScheme) {
    final details = <String, String?>{
      'Roof Type': listing.roofType,
      'Construction': listing.constructionPeriod,
      'Insulation': listing.insulationType,
      'Parking': listing.parkingType,
      'Orientation': listing.gardenOrientation,
      'Boiler': listing.cvBoilerBrand != null
          ? '${listing.cvBoilerBrand} (${listing.cvBoilerYear ?? "Unknown"})'
          : null,
      'Volume': listing.volumeM3 != null ? '${listing.volumeM3} m³' : null,
    };

    final validDetails = details.entries.where((e) => e.value != null).toList();

    if (validDetails.isEmpty) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Details',
          style: ValoraTypography.titleLarge.copyWith(
            color: colorScheme.onSurface,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: ValoraSpacing.md),
        Wrap(
          spacing: ValoraSpacing.md,
          runSpacing: ValoraSpacing.md,
          children: validDetails
              .map(
                (e) => ValoraGlassContainer(
                  padding: const EdgeInsets.symmetric(
                    horizontal: ValoraSpacing.md,
                    vertical: ValoraSpacing.sm,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        e.key,
                        style: ValoraTypography.labelSmall.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        e.value!,
                        style: ValoraTypography.bodyMedium.copyWith(
                          color: colorScheme.onSurface,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              )
              .toList(),
        ),
      ],
    );
  }

  Widget _buildBrokerSection(ColorScheme colorScheme) {
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

  Widget _buildSpecItem(
    IconData icon,
    String label,
    String value,
    ColorScheme colorScheme,
  ) {
    return Container(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.5),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(ValoraSpacing.sm),
            decoration: BoxDecoration(
              color: colorScheme.surface,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 20, color: colorScheme.primary),
          ),
          const SizedBox(width: ValoraSpacing.md),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                label,
                style: ValoraTypography.labelSmall.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: ValoraTypography.titleMedium.copyWith(
                  color: colorScheme.onSurface,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          const SizedBox(width: ValoraSpacing.sm),
        ],
      ),
    );
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'new':
        return ValoraColors.newBadge;
      case 'sold':
      case 'under offer':
        return ValoraColors.soldBadge;
      default:
        return ValoraColors.primary;
    }
  }
}
