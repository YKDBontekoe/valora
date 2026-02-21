import 'package:flutter/material.dart';
import '../../models/property_detail.dart';
import '../common/valora_card.dart';
import '../../core/formatters/currency_formatter.dart';
import '../../core/theme/valora_colors.dart';

class ValuationCard extends StatelessWidget {
  final PropertyDetail property;

  const ValuationCard({super.key, required this.property});

  @override
  Widget build(BuildContext context) {
    if (property.pricePerM2 == null) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: ValoraCard(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Valuation Context', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                _buildMetric(context, 'Price / m²', property.pricePerM2!),
                if (property.neighborhoodAvgPriceM2 != null)
                  _buildMetric(context, 'Neighborhood Avg', property.neighborhoodAvgPriceM2!),
              ],
            ),
            if (property.pricePercentile != null) ...[
              const SizedBox(height: 16),
              LinearProgressIndicator(
                value: property.pricePercentile! / 100,
                backgroundColor: Colors.grey[200],
                color: ValoraColors.primary,
                minHeight: 8,
                borderRadius: BorderRadius.circular(4),
              ),
              const SizedBox(height: 4),
              Text(
                'Cheaper than ${property.pricePercentile!.toStringAsFixed(0)}% of nearby properties',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildMetric(BuildContext context, String label, double value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: Theme.of(context).textTheme.bodyMedium),
        Text(
          CurrencyFormatter.format(value),
          style: Theme.of(context).textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.bold,
                color: ValoraColors.primary,
              ),
        ),
      ],
    );
  }
}
