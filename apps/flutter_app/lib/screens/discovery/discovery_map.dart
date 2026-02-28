import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import '../../models/listing.dart';
import '../../core/theme/valora_colors.dart';

class DiscoveryMap extends StatefulWidget {
  final List<Listing> listings;

  const DiscoveryMap({super.key, required this.listings});

  @override
  State<DiscoveryMap> createState() => _DiscoveryMapState();
}

class _DiscoveryMapState extends State<DiscoveryMap> {
  final MapController _mapController = MapController();

  @override
  Widget build(BuildContext context) {
    // Center map roughly on Netherlands if no listings, else first listing
    final initialCenter = widget.listings.isNotEmpty &&
            widget.listings.first.latitude != null &&
            widget.listings.first.longitude != null
        ? LatLng(widget.listings.first.latitude!,
            widget.listings.first.longitude!)
        : const LatLng(52.1326, 5.2913);

    return FlutterMap(
      mapController: _mapController,
      options: MapOptions(
        initialCenter: initialCenter,
        initialZoom: 12.0,
      ),
      children: [
        TileLayer(
          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
          userAgentPackageName: 'com.valora.app',
        ),
        MarkerLayer(
          markers: widget.listings
              .where((l) => l.latitude != null && l.longitude != null)
              .map(
                (l) => Marker(
                  point: LatLng(l.latitude!, l.longitude!),
                  width: 40,
                  height: 40,
                  child: const Icon(
                    Icons.location_on,
                    color: ValoraColors.primary,
                    size: 40,
                  ),
                ),
              )
              .toList(),
        ),
      ],
    );
  }
}
