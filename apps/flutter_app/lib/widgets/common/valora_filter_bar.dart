import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/formatters/currency_formatter.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../providers/search_listings_provider.dart';
import '../valora_widgets.dart';

class ValoraFilterBar extends StatelessWidget {
  const ValoraFilterBar({
    super.key,
    required this.onFilterTap,
    required this.onSortTap,
  });

  final VoidCallback onFilterTap;
  final VoidCallback onSortTap;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Consumer<SearchListingsProvider>(
      builder: (context, provider, _) {
        if (!provider.hasActiveFiltersOrSort) {
          return const SizedBox.shrink();
        }

        return SizedBox(
          height: 40,
          child: ListView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),
            children: [
              if (provider.minPrice != null || provider.maxPrice != null)
                _buildChip(
                  context,
                  label: _priceChipLabel(provider.minPrice, provider.maxPrice),
                  onTap: onFilterTap,
                  onDelete: () => _handleDelete(context, provider.clearPriceFilter),
                ),
              if (provider.city != null)
                _buildChip(
                  context,
                  label: 'City: ${provider.city}',
                  onTap: onFilterTap,
                  onDelete: () => _handleDelete(context, provider.clearCityFilter),
                ),
              if (provider.minBedrooms != null)
                _buildChip(
                  context,
                  label: '${provider.minBedrooms}+ Beds',
                  onTap: onFilterTap,
                  onDelete: () => _handleDelete(context, provider.clearBedroomsFilter),
                ),
              if (provider.minLivingArea != null)
                _buildChip(
                  context,
                  label: '${provider.minLivingArea}+ m²',
                  onTap: onFilterTap,
                  onDelete: () => _handleDelete(context, provider.clearLivingAreaFilter),
                ),
              if (provider.minCompositeScore != null)
                _buildChip(
                  context,
                  label: 'Composite: ${provider.minCompositeScore}+',
                  onTap: onFilterTap,
                  onDelete: () => _handleDelete(context, provider.clearCompositeScoreFilter),
                ),
              if (provider.minSafetyScore != null)
                _buildChip(
                  context,
                  label: 'Safety: ${provider.minSafetyScore}+',
                  onTap: onFilterTap,
                  onDelete: () => _handleDelete(context, provider.clearSafetyScoreFilter),
                ),
              if (provider.isSortActive)
                _buildChip(
                  context,
                  label: _sortChipLabel(provider.sortBy, provider.sortOrder),
                  onTap: onSortTap,
                  onDelete: () => _handleDelete(context, provider.clearSort),
                ),
              if (provider.hasActiveFiltersOrSort)
                Padding(
                  padding: const EdgeInsets.only(left: 4),
                  child: IconButton(
                    icon: const Icon(Icons.clear_all_rounded, size: 20),
                    tooltip: 'Clear Filters',
                    style: IconButton.styleFrom(
                      backgroundColor: isDark
                          ? ValoraColors.surfaceVariantDark
                          : ValoraColors.surfaceVariantLight,
                    ),
                    onPressed: () => provider.clearFilters(),
                  ),
                ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildChip(
    BuildContext context, {
    required String label,
    required VoidCallback onTap,
    required VoidCallback onDelete,
  }) {
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: ValoraChip(
        label: label,
        isSelected: true,
        onSelected: (_) => onTap(),
        onDeleted: onDelete,
      ),
    );
  }

  Future<void> _handleDelete(
    BuildContext context,
    Future<void> Function() action,
  ) async {
    try {
      await action();
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Failed to clear filter')),
        );
      }
    }
  }

  String _priceChipLabel(double? minPrice, double? maxPrice) {
    final min = CurrencyFormatter.formatEur(minPrice ?? 0);
    final max = maxPrice != null ? CurrencyFormatter.formatEur(maxPrice) : 'Any';
    return 'Price: $min - $max';
  }

  String _sortChipLabel(String? sortBy, String? sortOrder) {
    switch (sortBy) {
      case 'price':
        return 'Price: ${sortOrder == 'asc' ? 'Low to High' : 'High to Low'}';
      case 'livingarea':
        return 'Area: ${sortOrder == 'asc' ? 'Small to Large' : 'Large to Small'}';
      case 'contextcompositescore':
        return 'Composite';
      case 'contextsafetyscore':
        return 'Safety';
      default:
        return 'Sort';
    }
  }
}
