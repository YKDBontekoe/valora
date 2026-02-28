import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/discovery_provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../widgets/common/valora_button.dart';

class DiscoveryFilters extends StatelessWidget {
  const DiscoveryFilters({super.key});

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<DiscoveryProvider>();

    return Padding(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Filters', style: ValoraTypography.headlineSmall),
          const SizedBox(height: ValoraSpacing.md),

          // City
          TextField(
            decoration: const InputDecoration(labelText: 'City'),
            onChanged: (val) {
              // Usually debounced or submitted on edit complete
              if (val.isEmpty) {
                provider.setCity(null);
              }
            },
            onSubmitted: (val) {
              provider.setCity(val.isEmpty ? null : val);
            },
          ),
          const SizedBox(height: ValoraSpacing.md),

          // Property Type
          Text('Property Type', style: ValoraTypography.titleMedium),
          Wrap(
            spacing: ValoraSpacing.sm,
            children: [
              'Appartement',
              'Woonhuis',
            ].map((type) {
              final isSelected = provider.propertyType == type;
              return FilterChip(
                label: Text(type),
                selected: isSelected,
                onSelected: (selected) {
                  provider.setPropertyType(selected ? type : null);
                },
              );
            }).toList(),
          ),
          const SizedBox(height: ValoraSpacing.md),

          // Energy Label
          Text('Energy Label', style: ValoraTypography.titleMedium),
          Wrap(
            spacing: ValoraSpacing.sm,
            children: ['A', 'B', 'C', 'D', 'E', 'F', 'G'].map((label) {
              final isSelected = provider.energyLabel == label;
              return FilterChip(
                label: Text(label),
                selected: isSelected,
                onSelected: (selected) {
                  provider.setEnergyLabel(selected ? label : null);
                },
              );
            }).toList(),
          ),
          const SizedBox(height: ValoraSpacing.md),

          // Sort By
          Text('Sort By', style: ValoraTypography.titleMedium),
          DropdownButton<String>(
            value: provider.sortBy,
            isExpanded: true,
            items: const [
              DropdownMenuItem(value: 'newest', child: Text('Newest')),
              DropdownMenuItem(value: 'price', child: Text('Price (Lowest)')),
              DropdownMenuItem(
                  value: 'pricepersqm', child: Text('Price / m² (Lowest)')),
              DropdownMenuItem(
                  value: 'relevance', child: Text('Relevance / Score')),
            ],
            onChanged: (value) {
              if (value != null) provider.setSortBy(value);
            },
          ),

          const Spacer(),
          ValoraButton(
            label: 'Clear Filters',

            isFullWidth: true,
            onPressed: () {
              provider.clearFilters();
              Navigator.pop(context);
            },
          ),
          const SizedBox(height: ValoraSpacing.sm),
          ValoraButton(
            label: 'Apply Filters',
            isFullWidth: true,
            onPressed: () => Navigator.pop(context),
          ),
        ],
      ),
    );
  }
}
