import 'package:flutter/material.dart';
import '../../models/property_detail.dart';
import '../common/valora_card.dart';
import '../../core/theme/valora_colors.dart';

class PropertyStats extends StatelessWidget {
  final PropertyDetail property;

  const PropertyStats({super.key, required this.property});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16.0),
      child: ValoraCard(
        padding: const EdgeInsets.all(16.0),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: [
            _buildStat(context, Icons.bed, '${property.bedrooms ?? '-'}', 'Beds'),
            _buildStat(context, Icons.bathtub, '${property.bathrooms ?? '-'}', 'Baths'),
            _buildStat(context, Icons.square_foot, '${property.livingAreaM2 ?? '-'} m²', 'Living'),
            if (property.energyLabel != null)
              _buildStat(context, Icons.energy_savings_leaf, property.energyLabel!, 'Energy'),
          ],
        ),
      ),
    );
  }

  Widget _buildStat(BuildContext context, IconData icon, String value, String label) {
    return Column(
      children: [
        Icon(icon, color: ValoraColors.accent),
        const SizedBox(height: 4),
        Text(
          value,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
        ),
        Text(
          label,
          style: Theme.of(context).textTheme.bodySmall,
        ),
      ],
    );
  }
}
