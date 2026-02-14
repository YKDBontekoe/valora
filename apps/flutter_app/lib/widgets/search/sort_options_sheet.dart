import 'package:flutter/material.dart';

import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../providers/search_listings_provider.dart';
import '../valora_glass_container.dart';

class SortOptionsSheet extends StatelessWidget {
  final SearchListingsProvider provider;
  final VoidCallback onClose;

  const SortOptionsSheet({
    super.key,
    required this.provider,
    required this.onClose,
  });

  Widget _buildSortOption(
    BuildContext context,
    String label,
    String sortBy,
    String sortOrder,
    String? currentSortBy,
    String? currentSortOrder,
  ) {
    final effectiveSortBy = currentSortBy ?? 'date';
    final effectiveSortOrder = currentSortOrder ?? 'desc';
    final isSelected =
        effectiveSortBy == sortBy && effectiveSortOrder == sortOrder;

    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),
      title: Text(
        label,
        style: isSelected
            ? ValoraTypography.bodyLarge.copyWith(
                color: ValoraColors.primary,
                fontWeight: FontWeight.bold,
              )
            : ValoraTypography.bodyLarge,
      ),
      trailing: isSelected
          ? const Icon(Icons.check_rounded, color: ValoraColors.primary)
          : null,
      onTap: () {
        provider.applyFilters(
          minPrice: provider.minPrice,
          maxPrice: provider.maxPrice,
          city: provider.city,
          minBedrooms: provider.minBedrooms,
          minLivingArea: provider.minLivingArea,
          maxLivingArea: provider.maxLivingArea,
          minSafetyScore: provider.minSafetyScore,
          minCompositeScore: provider.minCompositeScore,
          sortBy: sortBy,
          sortOrder: sortOrder,
        );
        onClose();
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final mediaQuery = MediaQuery.of(context);
    final maxSheetHeight = mediaQuery.size.height * 0.8;

    return ValoraGlassContainer(
      borderRadius: BorderRadius.vertical(
        top: Radius.circular(ValoraSpacing.radiusXl),
      ),
      child: ConstrainedBox(
        constraints: BoxConstraints(maxHeight: maxSheetHeight),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: EdgeInsets.all(ValoraSpacing.lg),
              child: Text(
                'Sort By',
                style: ValoraTypography.titleLarge,
                textAlign: TextAlign.center,
              ),
            ),
            Flexible(
              child: ListView(
                padding: EdgeInsets.zero,
                shrinkWrap: true,
                children: [
                  _buildSortOption(
                    context,
                    'Newest',
                    'date',
                    'desc',
                    provider.sortBy,
                    provider.sortOrder,
                  ),
                  _buildSortOption(
                    context,
                    'Price: Low to High',
                    'price',
                    'asc',
                    provider.sortBy,
                    provider.sortOrder,
                  ),
                  _buildSortOption(
                    context,
                    'Price: High to Low',
                    'price',
                    'desc',
                    provider.sortBy,
                    provider.sortOrder,
                  ),
                  _buildSortOption(
                    context,
                    'Area: Small to Large',
                    'livingarea',
                    'asc',
                    provider.sortBy,
                    provider.sortOrder,
                  ),
                  _buildSortOption(
                    context,
                    'Area: Large to Small',
                    'livingarea',
                    'desc',
                    provider.sortBy,
                    provider.sortOrder,
                  ),
                  _buildSortOption(
                    context,
                    'Composite Score: High to Low',
                    'contextcompositescore',
                    'desc',
                    provider.sortBy,
                    provider.sortOrder,
                  ),
                  _buildSortOption(
                    context,
                    'Safety Score: High to Low',
                    'contextsafetyscore',
                    'desc',
                    provider.sortBy,
                    provider.sortOrder,
                  ),
                  SizedBox(height: ValoraSpacing.xl),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
