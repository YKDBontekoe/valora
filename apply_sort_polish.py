import sys

with open('apps/flutter_app/lib/widgets/search/sort_options_sheet.dart', 'r') as f:
    content = f.read()

# Update _buildSortOption to include an icon
search_pattern = '    String? currentSortOrder,\n  ) {'
replacement = search_pattern + """
    IconData icon;
    switch (sortBy) {
      case 'date': icon = Icons.calendar_today_rounded; break;
      case 'price': icon = sortOrder == 'asc' ? Icons.arrow_upward_rounded : Icons.arrow_downward_rounded; break;
      case 'livingarea': icon = sortOrder == 'asc' ? Icons.straighten_rounded : Icons.aspect_ratio_rounded; break;
      case 'contextcompositescore': icon = Icons.star_rounded; break;
      case 'contextsafetyscore': icon = Icons.shield_rounded; break;
      default: icon = Icons.sort_rounded;
    }
"""
content = content.replace(search_pattern, replacement)

# Update ListTile to include leading icon
listtile_pattern = '    return ListTile(\n      contentPadding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),\n      title: Text('
replacement = """    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),
      leading: Icon(
        icon,
        color: isSelected ? ValoraColors.primary : ValoraColors.neutral500,
      ),
      title: Text("""
# Since the regex might be hard, I'll use a simpler replace
content = content.replace(
    'return ListTile(\n      contentPadding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),\n      title: Text(',
    'return ListTile(\n      contentPadding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),\n      leading: Icon(icon, color: isSelected ? ValoraColors.primary : ValoraColors.neutral500),\n      title: Text('
)

# Actually, I'll just rewrite the _buildSortOption method

sort_option_replacement = """
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

    IconData icon;
    switch (sortBy) {
      case 'date': icon = Icons.calendar_today_rounded; break;
      case 'price': icon = sortOrder == 'asc' ? Icons.trending_up_rounded : Icons.trending_down_rounded; break;
      case 'livingarea': icon = sortOrder == 'asc' ? Icons.square_foot_rounded : Icons.zoom_out_map_rounded; break;
      case 'contextcompositescore': icon = Icons.analytics_rounded; break;
      case 'contextsafetyscore': icon = Icons.security_rounded; break;
      default: icon = Icons.sort_rounded;
    }

    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),
      leading: Container(
        padding: const EdgeInsets.all(8),
        decoration: BoxDecoration(
          color: isSelected ? ValoraColors.primary.withValues(alpha: 0.1) : Colors.transparent,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Icon(
          icon,
          size: 20,
          color: isSelected ? ValoraColors.primary : ValoraColors.neutral500,
        ),
      ),
      title: Text(
        label,
        style:
            isSelected
                ? ValoraTypography.bodyLarge.copyWith(
                  color: ValoraColors.primary,
                  fontWeight: FontWeight.bold,
                )
                : ValoraTypography.bodyLarge,
      ),
      trailing:
          isSelected
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
"""

import re
content = re.sub(r'Widget _buildSortOption\(.*?\n    \);\n  }', sort_option_replacement, content, flags=re.DOTALL)

with open('apps/flutter_app/lib/widgets/search/sort_options_sheet.dart', 'w') as f:
    f.write(content)
