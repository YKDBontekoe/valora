import 'dart:developer' as developer;

import 'package:flutter/material.dart';

import '../../core/formatters/currency_formatter.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../providers/search_listings_provider.dart';
import '../valora_widgets.dart';

class ActiveFiltersList extends StatelessWidget {
  final SearchListingsProvider provider;
  final VoidCallback onFilterTap;
  final VoidCallback onSortTap;

  const ActiveFiltersList({
    super.key,
    required this.provider,
    required this.onFilterTap,
    required this.onSortTap,
  });

  String _priceChipLabel(double? minPrice, double? maxPrice) {
    final min = CurrencyFormatter.formatEur(minPrice ?? 0);
    final max = maxPrice != null
        ? CurrencyFormatter.formatEur(maxPrice)
        : 'Any';
    return 'Price: $min - $max';
  }

  String _areaChipLabel(int? minArea, int? maxArea) {
    final min = minArea != null ? '$minArea' : '0';
    final max = maxArea != null ? '$maxArea' : 'Any';
    return 'Area: $min - $max m²';
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

  void _handleError(BuildContext context, Object error, StackTrace stack, String message) {
    developer.log(
      message,
      name: 'ActiveFiltersList',
      error: error,
      stackTrace: stack,
    );
    if (context.mounted) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final bool isDark = Theme.of(context).brightness == Brightness.dark;

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
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ValoraChip(
                label: _priceChipLabel(provider.minPrice, provider.maxPrice),
                isSelected: true,
                onSelected: (_) => onFilterTap(),
                onDeleted:
                    () => provider.clearPriceFilter().catchError((e, stack) {
                      if (context.mounted) {
                        _handleError(
                          context,
                          e,
                          stack,
                          'Failed to clear price filter',
                        );
                      }
                    }),
              ),
            ),
          if (provider.city != null)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ValoraChip(
                label: 'City: ${provider.city}',
                isSelected: true,
                onSelected: (_) => onFilterTap(),
                onDeleted:
                    () => provider.clearCityFilter().catchError((e, stack) {
                      if (context.mounted) {
                        _handleError(
                          context,
                          e,
                          stack,
                          'Failed to clear city filter',
                        );
                      }
                    }),
              ),
            ),
          if (provider.minBedrooms != null)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ValoraChip(
                label: '${provider.minBedrooms}+ Beds',
                isSelected: true,
                onSelected: (_) => onFilterTap(),
                onDeleted:
                    () => provider.clearBedroomsFilter().catchError((e, stack) {
                      if (context.mounted) {
                        _handleError(
                          context,
                          e,
                          stack,
                          'Failed to clear bedrooms filter',
                        );
                      }
                    }),
              ),
            ),
          if (provider.minLivingArea != null ||
              provider.maxLivingArea != null)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ValoraChip(
                label: _areaChipLabel(
                  provider.minLivingArea,
                  provider.maxLivingArea,
                ),
                isSelected: true,
                onSelected: (_) => onFilterTap(),
                onDeleted:
                    () => provider.clearLivingAreaFilter().catchError((
                      e,
                      stack,
                    ) {
                      if (context.mounted) {
                        _handleError(
                          context,
                          e,
                          stack,
                          'Failed to clear area filter',
                        );
                      }
                    }),
              ),
            ),
          if (provider.minCompositeScore != null)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ValoraChip(
                label: 'Composite: ${provider.minCompositeScore}+',
                isSelected: true,
                onSelected: (_) => onFilterTap(),
                onDeleted:
                    () => provider.clearCompositeScoreFilter().catchError((
                      e,
                      stack,
                    ) {
                      if (context.mounted) {
                        _handleError(
                          context,
                          e,
                          stack,
                          'Failed to clear composite score filter',
                        );
                      }
                    }),
              ),
            ),
          if (provider.minSafetyScore != null)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ValoraChip(
                label: 'Safety: ${provider.minSafetyScore}+',
                isSelected: true,
                onSelected: (_) => onFilterTap(),
                onDeleted:
                    () => provider.clearSafetyScoreFilter().catchError((
                      e,
                      stack,
                    ) {
                      if (context.mounted) {
                        _handleError(
                          context,
                          e,
                          stack,
                          'Failed to clear safety score filter',
                        );
                      }
                    }),
              ),
            ),
          if (provider.isSortActive)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ValoraChip(
                label: _sortChipLabel(provider.sortBy, provider.sortOrder),
                isSelected: true,
                onSelected: (_) => onSortTap(),
                onDeleted:
                    () => provider.clearSort().catchError((e, stack) {
                      if (context.mounted) {
                        _handleError(
                          context,
                          e,
                          stack,
                          'Failed to clear sort',
                        );
                      }
                    }),
              ),
            ),
          if (provider.hasActiveFiltersOrSort)
            Padding(
              padding: const EdgeInsets.only(left: 4),
              child: IconButton(
                icon: const Icon(Icons.clear_all_rounded, size: 20),
                tooltip: 'Clear Filters',
                style: IconButton.styleFrom(
                  backgroundColor:
                      isDark
                          ? ValoraColors.surfaceVariantDark
                          : ValoraColors.surfaceVariantLight,
                ),
                onPressed: () => provider.clearFilters(),
              ),
            ),
        ],
      ),
    );
  }
}
