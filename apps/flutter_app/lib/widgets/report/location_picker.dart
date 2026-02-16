import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../valora_widgets.dart';

class LocationPicker extends StatefulWidget {
  const LocationPicker({
    super.key,
    this.initialCenter = const LatLng(52.3676, 4.9041), // Amsterdam
    this.initialZoom = 13.0,
  });

  final LatLng initialCenter;
  final double initialZoom;

  @override
  State<LocationPicker> createState() => _LocationPickerState();
}

class _LocationPickerState extends State<LocationPicker> {
  late final MapController _mapController;
  LatLng? _selectedPoint;

  @override
  void initState() {
    super.initState();
    _mapController = MapController();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Pick Location'),
        actions: [
          if (_selectedPoint != null)
            TextButton(
              onPressed: () => Navigator.pop(context, _selectedPoint),
              child: const Text('Confirm'),
            ),
        ],
      ),
      body: Stack(
        children: [
          FlutterMap(
            mapController: _mapController,
            options: MapOptions(
              initialCenter: widget.initialCenter,
              initialZoom: widget.initialZoom,
              onTap: (_, point) {
                setState(() {
                  _selectedPoint = point;
                });
              },
            ),
            children: [
              TileLayer(
                urlTemplate: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
                subdomains: const ['a', 'b', 'c'],
              ),
              if (_selectedPoint != null)
                MarkerLayer(
                  markers: [
                    Marker(
                      point: _selectedPoint!,
                      width: 40,
                      height: 40,
                      child: const Icon(
                        Icons.location_on_rounded,
                        color: ValoraColors.primary,
                        size: 40,
                      ),
                    ),
                  ],
                ),
            ],
          ),
          Positioned(
            bottom: 24,
            left: 24,
            right: 24,
            child: ValoraCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text(
                    'Tap on the map to select a location',
                    style: TextStyle(fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    _selectedPoint == null
                        ? 'No location selected'
                        : '${_selectedPoint!.latitude.toStringAsFixed(4)}, ${_selectedPoint!.longitude.toStringAsFixed(4)}',
                    style: const TextStyle(color: ValoraColors.neutral600),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
