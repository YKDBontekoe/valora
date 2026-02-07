import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../home_components.dart';

class HomeSliverAppBar extends StatelessWidget {
  final TextEditingController searchController;
  final ValueChanged<String> onSearchChanged;
  final VoidCallback onFilterPressed;
  final int activeFilterCount;
  final int unreadCount;
  final VoidCallback? onNotificationsPressed;
  final VoidCallback? onProfilePressed;
  final String? userInitials;

  const HomeSliverAppBar({
    super.key,
    required this.searchController,
    required this.onSearchChanged,
    required this.onFilterPressed,
    this.activeFilterCount = 0,
    this.unreadCount = 0,
    this.onNotificationsPressed,
    this.onProfilePressed,
    this.userInitials,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return SliverAppBar(
      pinned: true,
      backgroundColor: isDark
          ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
          : ValoraColors.backgroundLight.withValues(alpha: 0.95),
      surfaceTintColor: Colors.transparent,
      titleSpacing: 24,
      toolbarHeight: 70, // Height for the top row (Title + Profile)
      title: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            'Valora',
            style: ValoraTypography.headlineMedium.copyWith(
              color: ValoraColors.primary,
              fontWeight: FontWeight.bold,
              letterSpacing: -0.5,
            ),
          ),
          Row(
            children: [
              Stack(
                children: [
                  IconButton(
                    onPressed: onNotificationsPressed,
                    icon: Icon(
                      Icons.notifications_none_rounded,
                      color: isDark
                          ? ValoraColors.neutral400
                          : ValoraColors.neutral500,
                    ),
                  ),
                  if (unreadCount > 0)
                    Positioned(
                      top: 12,
                      right: 12,
                      child: Container(
                        width: 8,
                        height: 8,
                        decoration: const BoxDecoration(
                          color: ValoraColors.error,
                          shape: BoxShape.circle,
                        ),
                      ),
                    ),
                ],
              ),
              const SizedBox(width: 8),
              GestureDetector(
                onTap: onProfilePressed,
                child: Container(
                  width: 36,
                  height: 36,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    gradient: const LinearGradient(
                      colors: [ValoraColors.primary, ValoraColors.primaryLight],
                      begin: Alignment.bottomLeft,
                      end: Alignment.topRight,
                    ),
                    border: Border.all(
                      color: isDark
                          ? ValoraColors.surfaceDark
                          : ValoraColors.surfaceLight,
                      width: 2,
                    ),
                  ),
                  child: Center(
                    child: Text(
                      userInitials ?? '?',
                      style: const TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.bold,
                        fontSize: 14,
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
      bottom: PreferredSize(
        preferredSize: const Size.fromHeight(140),
        child: HomeHeader(
          searchController: searchController,
          onSearchChanged: onSearchChanged,
          onFilterPressed: onFilterPressed,
          activeFilterCount: activeFilterCount,
        ),
      ),
    );
  }
}
