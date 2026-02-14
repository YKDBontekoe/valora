import 'package:flutter/material.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';

class ListingDetailedFeatures extends StatelessWidget {
  const ListingDetailedFeatures({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final combinedFeatures = <MapEntry<String, String>>[
      ...listing.features.entries,
      ..._extractContextFeatures(listing),
    ];

    if (combinedFeatures.isEmpty) return const SizedBox.shrink();

    final colorScheme = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Features',
          style: ValoraTypography.titleLarge.copyWith(
            color: colorScheme.onSurface,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: ValoraSpacing.md),
        ...combinedFeatures.map(
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
                      style: ValoraTypography.bodyMedium.copyWith(
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
    );
  }

  List<MapEntry<String, String>> _extractContextFeatures(Listing listing) {
    final report = listing.contextReport;
    if (report == null) {
      return const <MapEntry<String, String>>[];
    }

    final metrics = <MapEntry<String, String>>[];
    final categories = <String>[
      'socialMetrics',
      'crimeMetrics',
      'demographicsMetrics',
      'housingMetrics',
      'mobilityMetrics',
      'amenityMetrics',
      'environmentMetrics',
    ];

    for (final category in categories) {
      final values = report[category];
      if (values is! List) {
        continue;
      }

      for (final item in values) {
        if (item is! Map) {
          continue;
        }
        final metric = item.map((key, value) => MapEntry(key.toString(), value));

        final label = metric['label']?.toString();
        final value = metric['value'];
        final unit = metric['unit']?.toString();
        if (label == null || value == null) {
          continue;
        }

        final formatted = value is num
            ? value % 1 == 0
                  ? value.toInt().toString()
                  : value.toStringAsFixed(1)
            : value.toString();
        metrics.add(MapEntry(label, unit == null || unit.isEmpty ? formatted : '$formatted $unit'));
      }
    }

    return metrics.take(10).toList(growable: false);
  }
}
