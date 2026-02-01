import 'dart:async';
import 'dart:math';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_map_marker_cluster/flutter_map_marker_cluster.dart';
import 'package:latlong2/latlong.dart' hide Path;
import 'package:provider/provider.dart';
import 'package:valora_app/core/theme/valora_colors.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:valora_app/screens/listing_detail_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/home_components.dart';

class MapSearchScreen extends StatefulWidget {
  final ApiService? apiService;

  const MapSearchScreen({super.key, this.apiService});

  @override
  State<MapSearchScreen> createState() => _MapSearchScreenState();
}

class _MapSearchScreenState extends State<MapSearchScreen> with TickerProviderStateMixin {
  late final MapController _mapController;
  late final PageController _pageController;
  late ApiService _apiService;

  List<Listing> _listings = [];
  bool _isLoading = true;
  int _selectedIndex = -1;

  // Search & Filter
  final TextEditingController _searchController = TextEditingController();
  Timer? _debounce;

  // Default Center (San Francisco as per design reference, or Amsterdam if data dictates)
  // Design says SF, but data defaults to Amsterdam. Let's pick a middle ground or stick to one.
  // We'll use SF coordinates for the design reference fidelity, but generate points around it.
  static const LatLng _initialCenter = LatLng(37.7749, -122.4194);
  static const double _initialZoom = 12.0;

  @override
  void initState() {
    super.initState();
    _mapController = MapController();
    _pageController = PageController(viewportFraction: 0.85);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_listings.isEmpty && _isLoading) {
      _apiService = widget.apiService ?? Provider.of<ApiService>(context, listen: false);
      _loadListings();
    }
  }

  @override
  void dispose() {
    _mapController.dispose();
    _pageController.dispose();
    _searchController.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  Future<void> _loadListings() async {
    if (!mounted) return;
    setState(() => _isLoading = true);

    try {
      // Fetch a larger page size for the map to show "tons"
      final response = await _apiService.getListings(
        ListingFilter(
          page: 1,
          pageSize: 50, // "Tons" for demo purposes
          searchTerm: _searchController.text,
        ),
      );

      if (mounted) {
        setState(() {
          _listings = response.items;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to load map data: $e')),
        );
      }
    }
  }

  void _onSearchChanged(String query) {
    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 500), () {
      _loadListings();
    });
  }

  void _onMarkerTap(Listing listing, int index) {
    setState(() {
      _selectedIndex = index;
    });
    // Scroll carousel
    if (_pageController.hasClients) {
      _pageController.animateToPage(
        index,
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeInOut,
      );
    }
    // Center map
    final coord = _getCoordinate(listing);
    _mapController.move(coord, 14.0); // Zoom in slightly
  }

  void _onPageChanged(int index) {
    setState(() {
      _selectedIndex = index;
    });
    if (index >= 0 && index < _listings.length) {
      final listing = _listings[index];
      final coord = _getCoordinate(listing);
      _mapController.move(coord, _mapController.camera.zoom); // Keep zoom
    }
  }

  /// Deterministically generate a coordinate for a listing based on its ID (or hash code).
  /// This simulates backend having coordinates.
  LatLng _getCoordinate(Listing listing) {
    final seed = listing.id.hashCode;
    final random = Random(seed);

    // Spread around the center (~5km radius)
    // 1 degree lat ~ 111km. 5km ~ 0.045 degrees.
    const double radius = 0.045;

    final latOffset = (random.nextDouble() - 0.5) * 2 * radius;
    final lngOffset = (random.nextDouble() - 0.5) * 2 * radius;

    return LatLng(
      _initialCenter.latitude + latOffset,
      _initialCenter.longitude + lngOffset,
    );
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      resizeToAvoidBottomInset: false, // Don't resize map when keyboard opens
      body: Stack(
        children: [
          // 1. Map Layer
          FlutterMap(
            mapController: _mapController,
            options: MapOptions(
              initialCenter: _initialCenter,
              initialZoom: _initialZoom,
              minZoom: 5,
              maxZoom: 18,
              onTap: (_, __) {
                 if (_selectedIndex != -1) {
                   setState(() => _selectedIndex = -1);
                 }
                 FocusScope.of(context).unfocus();
              },
            ),
            children: [
              TileLayer(
                urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                userAgentPackageName: 'com.valora.app',
                // Optional: Use a dark map style if in dark mode, but OSM default is light.
                // We can use ColorFilter to invert for dark mode if desired, but let's stick to standard for now.
              ),
              MarkerClusterLayerWidget(
                options: MarkerClusterLayerOptions(
                  maxClusterRadius: 45,
                  size: const Size(40, 40),
                  alignment: Alignment.center,
                  padding: const EdgeInsets.all(50),
                  maxZoom: 15,
                  markers: _listings.asMap().entries.map((entry) {
                    final index = entry.key;
                    final listing = entry.value;
                    final isSelected = _selectedIndex == index;
                    return Marker(
                      point: _getCoordinate(listing),
                      width: isSelected ? 80 : 60,
                      height: isSelected ? 50 : 35,
                      child: GestureDetector(
                        onTap: () => _onMarkerTap(listing, index),
                        child: _buildMarker(listing, isSelected),
                      ),
                    );
                  }).toList(),
                  builder: (context, markers) {
                    return Container(
                      decoration: BoxDecoration(
                        color: ValoraColors.primary,
                        shape: BoxShape.circle,
                        border: Border.all(color: Colors.white, width: 2),
                        boxShadow: [
                           BoxShadow(
                             color: Colors.black.withValues(alpha: 0.2),
                             blurRadius: 5,
                           )
                        ]
                      ),
                      child: Center(
                        child: Text(
                          markers.length.toString(),
                          style: const TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),

          // 2. Fake fake map overlay to tint (optional from design)
          // Skipping strictly, but adding gradient for UI controls visibility if needed.

          // 3. Top Search & Chips
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: Container(
              padding: EdgeInsets.only(
                top: MediaQuery.of(context).padding.top + 16,
                left: 16,
                right: 16,
                bottom: 16,
              ),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    (isDark ? Colors.black : Colors.white).withValues(alpha: 0.8),
                    (isDark ? Colors.black : Colors.white).withValues(alpha: 0.0),
                  ],
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Search Bar
                  Container(
                    height: 50,
                    decoration: BoxDecoration(
                      color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
                      borderRadius: BorderRadius.circular(12),
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
                        const SizedBox(width: 16),
                        Icon(
                          Icons.search,
                          color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: TextField(
                            controller: _searchController,
                            onChanged: _onSearchChanged,
                            decoration: const InputDecoration(
                              hintText: 'Search city, zip...',
                              border: InputBorder.none,
                            ),
                          ),
                        ),
                        Container(
                          width: 1,
                          height: 24,
                          color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
                        ),
                        IconButton(
                          icon: const Icon(Icons.tune_rounded),
                          color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                          onPressed: () {
                            // Open filters
                          },
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),
                  // Chips
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: [
                        _buildChip(
                          label: 'Standard',
                          icon: Icons.map,
                          isActive: true,
                        ),
                        const SizedBox(width: 8),
                        _buildChip(
                          label: 'Price Density',
                          icon: Icons.bar_chart,
                          isActive: false,
                        ),
                        const SizedBox(width: 8),
                        _buildChip(
                          label: 'Safety',
                          icon: Icons.shield,
                          isActive: false,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),

          // 4. Right Side Actions
          Positioned(
            right: 16,
            top: MediaQuery.of(context).size.height * 0.4,
            child: Column(
              children: [
                Container(
                  decoration: BoxDecoration(
                    color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
                    borderRadius: BorderRadius.circular(12),
                    boxShadow: [
                       BoxShadow(
                         color: Colors.black.withValues(alpha: 0.1),
                         blurRadius: 8,
                       )
                    ],
                  ),
                  child: Column(
                    children: [
                      IconButton(
                        icon: const Icon(Icons.add),
                        onPressed: () {
                          final newZoom = _mapController.camera.zoom + 1;
                          _mapController.move(_mapController.camera.center, newZoom);
                        },
                      ),
                      Divider(height: 1, color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
                      IconButton(
                        icon: const Icon(Icons.remove),
                        onPressed: () {
                           final newZoom = _mapController.camera.zoom - 1;
                           _mapController.move(_mapController.camera.center, newZoom);
                        },
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                Container(
                  decoration: BoxDecoration(
                    color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
                    borderRadius: BorderRadius.circular(12),
                    boxShadow: [
                       BoxShadow(
                         color: Colors.black.withValues(alpha: 0.1),
                         blurRadius: 8,
                       )
                    ],
                  ),
                  child: IconButton(
                    icon: const Icon(Icons.my_location),
                    color: ValoraColors.primary,
                    onPressed: () {
                      _mapController.move(_initialCenter, 13);
                    },
                  ),
                ),
              ],
            ),
          ),

          // 5. Bottom Carousel
          if (_listings.isNotEmpty)
            Positioned(
              left: 0,
              right: 0,
              bottom: 16, // Lift above generic bottom nav or align with it
              height: 280, // Height for card
              child: PageView.builder(
                controller: _pageController,
                itemCount: _listings.length,
                onPageChanged: _onPageChanged,
                padEnds: true,
                itemBuilder: (context, index) {
                  final listing = _listings[index];
                  final isSelected = _selectedIndex == index;

                  return AnimatedScale(
                    scale: isSelected ? 1.0 : 0.9,
                    duration: const Duration(milliseconds: 200),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 4),
                      child: FeaturedListingCard(
                        listing: listing,
                        onTap: () {
                           Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => ListingDetailScreen(listing: listing),
                            ),
                          );
                        },
                      ),
                    ),
                  );
                },
              ),
            ),

           // Loading Indicator
           if (_isLoading)
             Center(
               child: Container(
                 padding: const EdgeInsets.all(16),
                 decoration: BoxDecoration(
                   color: (isDark ? Colors.black : Colors.white).withValues(alpha: 0.8),
                   borderRadius: BorderRadius.circular(16),
                 ),
                 child: const CircularProgressIndicator(),
               ),
             ),
        ],
      ),
    );
  }

  Widget _buildMarker(Listing listing, bool isSelected) {
    // Format price: $1.2M or $450k
    String priceText = 'N/A';
    if (listing.price != null) {
      if (listing.price! >= 1000000) {
        priceText = '\$${(listing.price! / 1000000).toStringAsFixed(1)}M';
      } else {
        priceText = '\$${(listing.price! / 1000).toStringAsFixed(0)}k';
      }
    }

    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: isSelected ? ValoraColors.primary : (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight),
            borderRadius: BorderRadius.circular(16),
            border: Border.all(
              color: isSelected ? ValoraColors.primary : (isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
              width: 1,
            ),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.2),
                blurRadius: 4,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (isSelected)
                const Padding(
                  padding: EdgeInsets.only(right: 4),
                  child: Icon(Icons.auto_awesome, size: 12, color: Colors.white),
                ),
              Text(
                priceText,
                style: TextStyle(
                  color: isSelected ? Colors.white : (isDark ? Colors.white : Colors.black),
                  fontWeight: FontWeight.bold,
                  fontSize: isSelected ? 12 : 11,
                ),
              ),
            ],
          ),
        ),
        // Triangle/Arrow
        ClipPath(
          clipper: TriangleClipper(),
          child: Container(
            color: isSelected ? ValoraColors.primary : (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight),
            width: 10,
            height: 6,
          ),
        ),
      ],
    );
  }

  Widget _buildChip({required String label, required IconData icon, required bool isActive}) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: isActive ? ValoraColors.primary : (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight),
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
           if (isActive)
             BoxShadow(
               color: ValoraColors.primary.withValues(alpha: 0.3),
               blurRadius: 8,
             )
        ],
      ),
      child: Row(
        children: [
          Icon(
            icon,
            size: 16,
            color: isActive ? Colors.white : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral600),
          ),
          const SizedBox(width: 6),
          Text(
            label,
            style: TextStyle(
              color: isActive ? Colors.white : (isDark ? ValoraColors.neutral200 : ValoraColors.neutral800),
              fontWeight: FontWeight.w600,
              fontSize: 12,
            ),
          ),
        ],
      ),
    );
  }
}

class TriangleClipper extends CustomClipper<Path> {
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
