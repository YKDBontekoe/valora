import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';

import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../providers/search_listings_provider.dart';
import '../../services/notification_service.dart';
import '../../services/pdok_service.dart';
import '../../screens/notifications_screen.dart';
import 'active_filters_list.dart';
import 'search_input.dart';

class SearchAppBar extends StatelessWidget {
  final TextEditingController searchController;
  final PdokService pdokService;
  final Function(PdokSuggestion) onSuggestionSelected;
  final VoidCallback onSubmitted;
  final VoidCallback onSortTap;
  final VoidCallback onFilterTap;

  const SearchAppBar({
    super.key,
    required this.searchController,
    required this.pdokService,
    required this.onSuggestionSelected,
    required this.onSubmitted,
    required this.onSortTap,
    required this.onFilterTap,
  });

  @override
  Widget build(BuildContext context) {
    final bool isDark = Theme.of(context).brightness == Brightness.dark;

    return Selector<SearchListingsProvider, bool>(
      selector: (_, p) => p.hasActiveFiltersOrSort,
      builder: (context, hasActiveFiltersOrSort, _) {
        return SliverAppBar(
          pinned: true,
          backgroundColor: isDark
              ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
              : ValoraColors.backgroundLight.withValues(alpha: 0.95),
          surfaceTintColor: Colors.transparent,
          title: Text(
            'Search',
            style: ValoraTypography.headlineMedium.copyWith(
              color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
              fontWeight: FontWeight.bold,
            ),
          ),
          centerTitle: false,
          actions: [
            Consumer<NotificationService>(
              builder: (context, notificationProvider, _) {
                return Stack(
                  children: [
                    IconButton(
                      onPressed: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => const NotificationsScreen(),
                          ),
                        );
                      },
                      icon: const Icon(Icons.notifications_outlined),
                      tooltip: 'Notifications',
                    ),
                    if (notificationProvider.unreadCount > 0)
                      Positioned(
                        top: ValoraSpacing.radiusLg,
                        right: ValoraSpacing.radiusLg,
                        child: Container(
                          width: ValoraSpacing.sm,
                          height: ValoraSpacing.sm,
                          decoration: const BoxDecoration(
                            color: ValoraColors.error,
                            shape: BoxShape.circle,
                          ),
                        ),
                      ).animate().scale(curve: Curves.elasticOut),
                  ],
                );
              },
            ),
            IconButton(
              onPressed: onSortTap,
              icon: const Icon(Icons.sort_rounded),
              tooltip: 'Sort',
            ),
            Stack(
              children: [
                IconButton(
                  onPressed: onFilterTap,
                  icon: const Icon(Icons.tune_rounded),
                  tooltip: 'Filters',
                ),
                Selector<SearchListingsProvider, bool>(
                  selector: (_, p) => p.hasActiveFilters,
                  builder: (context, hasActiveFilters, _) {
                    if (hasActiveFilters) {
                      return Positioned(
                        top: ValoraSpacing.sm,
                        right: ValoraSpacing.sm,
                        child: Container(
                          width: 10,
                          height: 10,
                          decoration: const BoxDecoration(
                            color: ValoraColors.primary,
                            shape: BoxShape.circle,
                          ),
                        ),
                      );
                    }
                    return const SizedBox.shrink();
                  },
                ),
              ],
            ),
            const SizedBox(width: ValoraSpacing.sm),
          ],
          bottom: PreferredSize(
            preferredSize: Size.fromHeight(
              hasActiveFiltersOrSort ? 130 : 80,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SearchInput(
                  controller: searchController,
                  pdokService: pdokService,
                  onSuggestionSelected: onSuggestionSelected,
                  onSubmitted: onSubmitted,
                ),
                Consumer<SearchListingsProvider>(
                  builder: (context, provider, _) => ActiveFiltersList(
                    provider: provider,
                    onFilterTap: onFilterTap,
                    onSortTap: onSortTap,
                  ),
                ),
                if (hasActiveFiltersOrSort)
                  const SizedBox(height: ValoraSpacing.radiusLg),
              ],
            ),
          ),
        );
      },
    );
  }
}
