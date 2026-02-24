import 'package:flutter/material.dart';
import 'package:latlong2/latlong.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../widgets/report/location_picker.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../services/pdok_service.dart';
import '../../../services/location_service.dart';
import '../../../providers/context_report_provider.dart';

class QuickActions extends StatelessWidget {
  const QuickActions({
    super.key,
    required this.pdokService,
    required this.provider,
    required this.controller,
    LocationService? locationService,
  }) : _locationService = locationService ?? const LocationService();

  final PdokService pdokService;
  final ContextReportProvider provider;
  final TextEditingController controller;
  final LocationService _locationService;

  Future<void> _pickLocation(BuildContext context) async {
    final LatLng? result = await Navigator.push<LatLng>(
      context,
      MaterialPageRoute(builder: (context) => const LocationPicker()),
    );

    if (!context.mounted) return;
    if (result != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('Resolving address…'),
          duration: const Duration(seconds: 1),
          behavior: SnackBarBehavior.floating,
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );

      final String? address =
          await pdokService.reverseLookup(result.latitude, result.longitude);

      if (!context.mounted) return;

      if (address != null) {
        controller.text = address;
        provider.generate(address);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text(
                'Could not resolve an address. Try searching by text.'),
            backgroundColor: ValoraColors.error,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12)),
          ),
        );
      }
    }
  }

  Future<void> _useMyLocation(BuildContext context) async {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: const Text('Getting location…'),
        duration: const Duration(seconds: 1),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    );

    try {
      final position = await _locationService.getCurrentLocation();

      if (!context.mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('Resolving address…'),
          duration: const Duration(seconds: 1),
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );

      final address = await pdokService.reverseLookup(
        position.latitude,
        position.longitude,
      );

      if (!context.mounted) return;

      if (address != null) {
        controller.text = address;
        provider.generate(address);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text('Could not resolve an address for your location.'),
            backgroundColor: ValoraColors.error,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
        );
      }
    } on ValoraLocationServiceDisabledException catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.toString()),
          backgroundColor: ValoraColors.error,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
    } on ValoraPermissionDeniedException catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.toString()),
          backgroundColor: ValoraColors.error,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
    } on ValoraPermissionDeniedForeverException catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.toString()),
          backgroundColor: ValoraColors.error,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
    } catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('Could not determine location.'),
          backgroundColor: ValoraColors.error,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        ValoraButton(
          label: 'Pick on Map',
          icon: Icons.map_rounded,
          variant: ValoraButtonVariant.secondary,
          size: ValoraButtonSize.small,
          onPressed: () => _pickLocation(context),
        ),
        const SizedBox(width: 10),
        ValoraButton(
          label: 'My Location',
          icon: Icons.gps_fixed_rounded,
          variant: ValoraButtonVariant.secondary,
          size: ValoraButtonSize.small,
          onPressed: () => _useMyLocation(context),
        ),
      ],
    );
  }
}
