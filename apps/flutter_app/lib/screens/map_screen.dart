import 'dart:math';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart' hide Path; // Hide Path to avoid conflict with dart:ui Path
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../providers/favorites_provider.dart';
import 'listing_detail_screen.dart';

class MapScreen extends StatefulWidget {
  final List<Listing> listings;

  const MapScreen({super.key, required this.listings});

  @override
  State<MapScreen> createState() => _MapScreenState();
}

class _MapScreenState extends State<MapScreen> {
  late final MapController _mapController;
  late final PageController _pageController;
  final LatLng _center = const LatLng(52.3676, 4.9041); // Amsterdam
  int _selectedIndex = -1;
  late List<LatLng> _listingLocations;

  @override
  void initState() {
    super.initState();
    _mapController = MapController();
    _pageController = PageController(viewportFraction: 0.85);
    _generateLocations();
  }

  void _generateLocations() {
    _listingLocations = widget.listings.map((listing) {
      // Generate a deterministic location around Amsterdam based on ID
      // If we used listing.id.hashCode directly it might be too scattered,
      // so we just use the index and a seeded random for the demo.
      // But to be consistent per listing ID, we should use the hash.
      final seed = listing.id.hashCode;
      final r = Random(seed);

      // Random offset within ~2km
      final latOffset = (r.nextDouble() - 0.5) * 0.04;
      final lngOffset = (r.nextDouble() - 0.5) * 0.06;

      return LatLng(_center.latitude + latOffset, _center.longitude + lngOffset);
    }).toList();
  }

  @override
  void dispose() {
    _mapController.dispose();
    _pageController.dispose();
    super.dispose();
  }

  void _onMarkerTap(int index) {
    setState(() {
      _selectedIndex = index;
    });
    _pageController.animateToPage(
      index,
      duration: const Duration(milliseconds: 300),
      curve: Curves.easeInOut,
    );
    _centerMap(_listingLocations[index]);
  }

  void _onPageChanged(int index) {
    setState(() {
      _selectedIndex = index;
    });
    _centerMap(_listingLocations[index]);
  }

  void _centerMap(LatLng location) {
    _mapController.move(location, 14.0);
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      body: Stack(
        children: [
          // 1. Map Layer
          FlutterMap(
            mapController: _mapController,
            options: MapOptions(
              initialCenter: _center,
              initialZoom: 13.0,
              interactionOptions: const InteractionOptions(
                flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
              ),
              onTap: (_, _) {
                if (_selectedIndex != -1) {
                  setState(() => _selectedIndex = -1);
                }
              },
            ),
            children: [
              TileLayer(
                urlTemplate: isDark
                    ? 'https://cartodb-basemaps-{s}.global.ssl.fastly.net/dark_all/{z}/{x}/{y}.png'
                    : 'https://cartodb-basemaps-{s}.global.ssl.fastly.net/light_all/{z}/{x}/{y}.png',
                userAgentPackageName: 'com.valora.app',
                subdomains: const ['a', 'b', 'c', 'd'],
              ),
              MarkerLayer(
                markers: List.generate(widget.listings.length, (index) {
                  final listing = widget.listings[index];
                  final isSelected = _selectedIndex == index;
                  return Marker(
                    point: _listingLocations[index],
                    width: 80,
                    height: 50,
                    child: GestureDetector(
                      onTap: () => _onMarkerTap(index),
                      child: _PriceMarker(
                        price: listing.price,
                        isSelected: isSelected,
                      ),
                    ),
                  );
                }),
              ),
            ],
          ),

          // 2. Top Overlay (Search & Filters)
          Positioned(
            top: MediaQuery.of(context).padding.top + 16,
            left: 16,
            right: 16,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Search Bar
                Container(
                  height: 48,
                  decoration: BoxDecoration(
                    color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
                    borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: 0.1),
                        blurRadius: 10,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  child: Row(
                    children: [
                      const SizedBox(width: 12),
                      Icon(
                        Icons.search_rounded,
                        color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          'Search neighborhood...',
                          style: ValoraTypography.bodyMedium.copyWith(
                            color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                          ),
                        ),
                      ),
                      Container(
                        width: 1,
                        height: 24,
                        color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
                      ),
                      IconButton(
                        onPressed: () {},
                        icon: const Icon(Icons.tune_rounded),
                        color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral600,
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                // Filter Buttons
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  clipBehavior: Clip.none,
                  child: Row(
                    children: [
                      _MapFilterChip(
                        icon: Icons.grid_view_rounded,
                        label: 'Standard',
                        isSelected: false,
                        onTap: () {},
                      ),
                      const SizedBox(width: 8),
                      _MapFilterChip(
                        icon: Icons.texture_rounded,
                        label: 'Price Density',
                        isSelected: true,
                        onTap: () {},
                      ),
                      const SizedBox(width: 8),
                      _MapFilterChip(
                        icon: Icons.verified_user_rounded,
                        label: 'Safety',
                        isSelected: false,
                        onTap: () {},
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),

          // 3. Right Side Controls (Zoom/Location) - Visual only for now
          Positioned(
            right: 16,
            top: MediaQuery.of(context).size.height * 0.4,
            child: Column(
              children: [
                _MapControlButton(
                  icon: Icons.add_rounded,
                  isTop: true,
                  onTap: () {
                     final currentZoom = _mapController.camera.zoom;
                     _mapController.move(_mapController.camera.center, currentZoom + 1);
                  },
                ),
                _MapControlButton(
                  icon: Icons.remove_rounded,
                  isBottom: true,
                  onTap: () {
                     final currentZoom = _mapController.camera.zoom;
                     _mapController.move(_mapController.camera.center, currentZoom - 1);
                  },
                ),
                const SizedBox(height: 12),
                _MapControlButton(
                  icon: Icons.my_location_rounded,
                  isSingle: true,
                  isActive: true,
                  onTap: () => _centerMap(_center),
                ),
              ],
            ),
          ),

          // 4. Bottom List Overlay
          if (_selectedIndex != -1)
            Positioned(
              left: 0,
              right: 0,
              bottom: 20 + MediaQuery.of(context).padding.bottom + 60, // +60 for Nav Bar space
              height: 280, // Height for the card
              child: PageView.builder(
                controller: _pageController,
                itemCount: widget.listings.length,
                onPageChanged: _onPageChanged,
                physics: const BouncingScrollPhysics(),
                itemBuilder: (context, index) {
                  final listing = widget.listings[index];
                  // Use the existing FeaturedListingCard but wrap it to fit styling
                  return Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 8.0),
                    child: GestureDetector(
                      onTap: () {
                        // Navigate to detail
                         Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => ListingDetailScreen(listing: listing),
                          ),
                        );
                      },
                      child: _MapListingCard(listing: listing),
                    ),
                  );
                },
              ),
            ),
        ],
      ),
    );
  }
}

class _PriceMarker extends StatelessWidget {
  final double? price;
  final bool isSelected;

  const _PriceMarker({required this.price, this.isSelected = false});

  @override
  Widget build(BuildContext context) {
    final priceStr = price != null
        ? '\$${(price! / 1000).toStringAsFixed(0)}k'
        : '?';

    final color = isSelected ? ValoraColors.primary : (Theme.of(context).brightness == Brightness.dark ? Colors.grey[800]! : Colors.white);
    final textColor = isSelected ? Colors.white : (Theme.of(context).brightness == Brightness.dark ? Colors.white : Colors.grey[900]);

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(16),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.2),
                blurRadius: 4,
                offset: const Offset(0, 2),
              ),
            ],
            border: isSelected ? null : Border.all(color: Colors.grey.withValues(alpha: 0.3)),
          ),
          child: Text(
            priceStr,
            style: TextStyle(
              color: textColor,
              fontWeight: FontWeight.bold,
              fontSize: 12,
            ),
          ),
        ),
        // Triangle pointer
        ClipPath(
          clipper: _TriangleClipper(),
          child: Container(
            width: 8,
            height: 6,
            color: color,
          ),
        ),
      ],
    );
  }
}

class _TriangleClipper extends CustomClipper<Path> {
  @override
  Path getClip(Size size) {
    final path = Path();
    path.moveTo(0, 0);
    path.lineTo(size.width / 2, size.height);
    path.lineTo(size.width, 0);
    path.close();
    return path;
  }

  @override
  bool shouldReclip(covariant CustomClipper<Path> oldClipper) => false;
}

class _MapFilterChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool isSelected;
  final VoidCallback onTap;

  const _MapFilterChip({
    required this.icon,
    required this.label,
    required this.isSelected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final bgColor = isSelected
        ? ValoraColors.primary.withValues(alpha: 0.1)
        : (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight);
    final borderColor = isSelected
        ? ValoraColors.primary.withValues(alpha: 0.2)
        : (isDark ? ValoraColors.neutral700 : Colors.transparent);
    final iconColor = isSelected
        ? ValoraColors.primary
        : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500);
    final textColor = isSelected
        ? ValoraColors.primary
        : (isDark ? ValoraColors.neutral300 : ValoraColors.neutral700);

    return GestureDetector(
      onTap: onTap,
      child: Container(
        height: 36,
        padding: const EdgeInsets.symmetric(horizontal: 12),
        decoration: BoxDecoration(
          color: bgColor,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: borderColor),
          boxShadow: isSelected ? [] : [
             BoxShadow(
              color: Colors.black.withValues(alpha: 0.05),
              blurRadius: 4,
            ),
          ],
        ),
        child: Row(
          children: [
            Icon(icon, size: 18, color: iconColor),
            const SizedBox(width: 6),
            Text(
              label,
              style: TextStyle(
                color: textColor,
                fontWeight: FontWeight.w600,
                fontSize: 13,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MapControlButton extends StatelessWidget {
  final IconData icon;
  final VoidCallback onTap;
  final bool isTop;
  final bool isBottom;
  final bool isSingle;
  final bool isActive;

  const _MapControlButton({
    required this.icon,
    required this.onTap,
    this.isTop = false,
    this.isBottom = false,
    this.isSingle = false,
    this.isActive = false,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 44,
        height: 44,
        decoration: BoxDecoration(
          color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
          borderRadius: isSingle
              ? BorderRadius.circular(12)
              : BorderRadius.vertical(
                  top: isTop ? const Radius.circular(12) : Radius.zero,
                  bottom: isBottom ? const Radius.circular(12) : Radius.zero,
                ),
          boxShadow: [
             BoxShadow(
              color: Colors.black.withValues(alpha: 0.1),
              blurRadius: 8,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Icon(
          icon,
          color: isActive ? ValoraColors.primary : (isDark ? ValoraColors.neutral200 : ValoraColors.neutral700),
          size: 20,
        ),
      ),
    );
  }
}

class _MapListingCard extends StatelessWidget {
  final Listing listing;

  const _MapListingCard({required this.listing});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final favoritesProvider = Provider.of<FavoritesProvider>(context);
    final isFavorite = favoritesProvider.isFavorite(listing.id);

    return Container(
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.1),
            blurRadius: 15,
            offset: const Offset(0, 5),
          ),
        ],
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          // Image Area
          Expanded(
            flex: 3,
            child: Stack(
              fit: StackFit.expand,
              children: [
                listing.imageUrl != null
                  ? CachedNetworkImage(
                      imageUrl: listing.imageUrl!,
                      fit: BoxFit.cover,
                    )
                  : Container(color: Colors.grey),
                Positioned(
                  top: 10,
                  left: 10,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.9),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Row(
                      children: const [
                        Icon(Icons.trending_down, size: 12, color: ValoraColors.primary),
                        SizedBox(width: 4),
                        Text(
                          'UNDER MARKET',
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                            color: ValoraColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                Positioned(
                  top: 10,
                  right: 10,
                  child: GestureDetector(
                    onTap: () => favoritesProvider.toggleFavorite(listing),
                    child: CircleAvatar(
                      backgroundColor: Colors.black.withValues(alpha: 0.3),
                      radius: 16,
                      child: Icon(
                        isFavorite ? Icons.favorite : Icons.favorite_border,
                        color: isFavorite ? Colors.red : Colors.white,
                        size: 18,
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          // Info Area
          Expanded(
            flex: 2,
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        listing.price != null ? '\$${listing.price!.toStringAsFixed(0)}' : 'Price on request',
                        style: ValoraTypography.titleMedium.copyWith(fontWeight: FontWeight.w800),
                      ),
                      const Text(
                        'ACTIVE',
                        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.grey),
                      ),
                    ],
                  ),
                  Text(
                    listing.address,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: ValoraTypography.bodySmall.copyWith(color: Colors.grey),
                  ),
                  const Divider(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      _buildMiniFeature(Icons.bed, '${listing.bedrooms ?? 0} bd'),
                      _buildMiniFeature(Icons.bathtub, '${listing.bathrooms ?? 0} ba'),
                      _buildMiniFeature(Icons.square_foot, '${listing.livingAreaM2 ?? 0} sqft'),
                    ],
                  )
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMiniFeature(IconData icon, String text) {
    return Row(
      children: [
        Icon(icon, size: 14, color: Colors.grey),
        const SizedBox(width: 4),
        Text(text, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Colors.grey)),
      ],
    );
  }
}
