import 'package:flutter/material.dart';
import '../../models/property_detail.dart';
import '../../core/formatters/currency_formatter.dart';
import '../../core/theme/valora_colors.dart';

class PropertyHeader extends StatelessWidget {
  final PropertyDetail property;

  const PropertyHeader({super.key, required this.property});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (property.price != null)
            Text(
              CurrencyFormatter.format(property.price!),
              style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: ValoraColors.primary,
                  ),
            ),
          const SizedBox(height: 8),
          Text(
            property.address,
            style: Theme.of(context).textTheme.titleLarge,
          ),
          if (property.city != null || property.postalCode != null)
            Text(
              '${property.postalCode ?? ''} ${property.city ?? ''}'.trim(),
              style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                    color: Colors.grey[600],
                  ),
            ),
        ],
      ),
    );
  }
}
