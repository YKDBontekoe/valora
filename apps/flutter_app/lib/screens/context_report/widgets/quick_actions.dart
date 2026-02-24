import 'package:flutter/material.dart';
import 'package:latlong2/latlong.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../widgets/report/location_picker.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../services/pdok_service.dart';
import '../../../providers/context_report_provider.dart';

class QuickActions extends StatelessWidget {
  const QuickActions({
    super.key,
    required this.pdokService,
    required this.provider,
    required this.controller,
  });

  final PdokService pdokService;
  final ContextReportProvider provider;
  final TextEditingController controller;

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
          onPressed: () {
            // TODO: implement current location lookup
          },
        ),
      ],
    );
  }
}
