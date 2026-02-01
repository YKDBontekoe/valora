import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../providers/theme_provider.dart';
import '../providers/auth_provider.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    // Access theme mode to conditionally render UI
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final backgroundColor = isDark ? ValoraColors.backgroundDark : ValoraColors.backgroundLight;
    final surfaceColor = isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight;
    final textColor = isDark ? ValoraColors.onBackgroundDark : ValoraColors.onBackgroundLight;
    final subtextColor = isDark ? ValoraColors.neutral400 : ValoraColors.neutral500;
    final borderColor = isDark ? ValoraColors.neutral800 : ValoraColors.neutral200;

    return Scaffold(
      backgroundColor: backgroundColor,
      body: CustomScrollView(
        slivers: [
          // Sticky Header
          SliverAppBar(
            pinned: true,
            backgroundColor: backgroundColor.withValues(alpha: 0.95),
            surfaceTintColor: Colors.transparent,
            elevation: 0,
            automaticallyImplyLeading: false, // No back button since it's a tab
            title: Text(
              'Settings',
              style: TextStyle(
                color: textColor,
                fontWeight: FontWeight.bold,
                fontSize: 28,
                letterSpacing: -0.5,
              ),
            ),
            actions: [
              Consumer<ThemeProvider>(
                builder: (context, themeProvider, _) {
                  final isDarkMode = themeProvider.isDarkMode;
                  return Padding(
                    padding: const EdgeInsets.only(right: 16.0),
                    child: IconButton(
                      onPressed: () {
                        themeProvider.toggleTheme();
                      },
                      icon: Icon(
                        isDarkMode ? Icons.light_mode_rounded : Icons.dark_mode_rounded,
                        color: subtextColor,
                      ),
                      style: IconButton.styleFrom(
                        backgroundColor: isDark ? ValoraColors.neutral800 : ValoraColors.neutral100,
                        shape: const CircleBorder(),
                      ),
                    ),
                  );
                },
              ),
            ],
          ),

          // Content
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 8.0),
              child: Column(
                children: [
                  // Profile Section
                  _buildProfileCard(context, surfaceColor, borderColor, textColor, subtextColor),

                  const SizedBox(height: 24),

                  // Preferences Section
                  _buildSectionHeader('PREFERENCES', subtextColor),
                  const SizedBox(height: 12),
                  Container(
                    decoration: BoxDecoration(
                      color: surfaceColor,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: borderColor),
                      boxShadow: [
                         BoxShadow(
                          color: Colors.black.withValues(alpha: 0.05),
                          blurRadius: 4,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Column(
                      children: [
                        _buildSettingsTile(
                          context,
                          icon: Icons.notifications_active_rounded,
                          iconColor: Colors.blue,
                          iconBgColor: Colors.blue.withValues(alpha: 0.1),
                          title: 'Smart Alerts',
                          subtitle: 'Instant updates on price drops',
                          showDivider: true,
                        ),
                        _buildSettingsTile(
                          context,
                          icon: Icons.tune_rounded,
                          iconColor: Colors.purple,
                          iconBgColor: Colors.purple.withValues(alpha: 0.1),
                          title: 'Search Preferences',
                          subtitle: 'Location, Price, Amenities',
                          showDivider: true,
                        ),
                        _buildSettingsTile(
                          context,
                          icon: Icons.palette_rounded,
                          iconColor: Colors.orange,
                          iconBgColor: Colors.orange.withValues(alpha: 0.1),
                          title: 'Appearance',
                          subtitle: 'Theme & Display settings',
                          showDivider: false,
                          onTap: () {
                             context.read<ThemeProvider>().toggleTheme();
                          },
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 24),

                  // Account & Security Section
                  _buildSectionHeader('ACCOUNT & SECURITY', subtextColor),
                  const SizedBox(height: 12),
                  Container(
                    decoration: BoxDecoration(
                      color: surfaceColor,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: borderColor),
                      boxShadow: [
                         BoxShadow(
                          color: Colors.black.withValues(alpha: 0.05),
                          blurRadius: 4,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Column(
                      children: [
                        _buildSettingsTile(
                          context,
                          icon: Icons.card_membership_rounded,
                          iconColor: ValoraColors.success,
                          iconBgColor: ValoraColors.success.withValues(alpha: 0.1),
                          title: 'Subscription',
                          subtitle: 'Pro Plan Active',
                          subtitleColor: ValoraColors.success,
                          showDivider: true,
                        ),
                        _buildSettingsTile(
                          context,
                          icon: Icons.lock_rounded,
                          iconColor: Colors.grey,
                          iconBgColor: Colors.grey.withValues(alpha: 0.1),
                          title: 'Privacy & Security',
                          subtitle: 'Password, FaceID',
                          showDivider: false,
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 32),

                  // Help Section
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: ValoraColors.primary.withValues(alpha: isDark ? 0.1 : 0.05),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: ValoraColors.primary.withValues(alpha: 0.2)),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Need Help?',
                              style: TextStyle(
                                color: isDark ? ValoraColors.primaryLight : ValoraColors.primary,
                                fontWeight: FontWeight.bold,
                                fontSize: 16,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              'Our support team is available 24/7',
                              style: TextStyle(
                                color: subtextColor,
                                fontSize: 12,
                              ),
                            ),
                          ],
                        ),
                        ElevatedButton(
                          onPressed: () {},
                          style: ElevatedButton.styleFrom(
                            backgroundColor: surfaceColor,
                            foregroundColor: textColor,
                            elevation: 0,
                            side: BorderSide(color: borderColor),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(8),
                            ),
                            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                          ),
                          child: const Text('Contact Us'),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 24),

                  // Logout
                  TextButton.icon(
                    onPressed: () {
                      context.read<AuthProvider>().logout();
                    },
                    icon: const Icon(Icons.logout_rounded, size: 20),
                    label: const Text('Log Out'),
                    style: TextButton.styleFrom(
                      foregroundColor: subtextColor,
                      textStyle: const TextStyle(fontWeight: FontWeight.w600),
                    ).copyWith(
                      foregroundColor: WidgetStateProperty.resolveWith((states) {
                        if (states.contains(WidgetState.hovered)) {
                          return ValoraColors.error;
                        }
                        return subtextColor;
                      }),
                    ),
                  ),

                  const SizedBox(height: 16),

                  Text(
                    'Valora v2.4.0 (Build 392)',
                    style: TextStyle(
                      color: subtextColor.withValues(alpha: 0.5),
                      fontSize: 12,
                    ),
                  ),

                  const SizedBox(height: 100), // Bottom padding for navigation bar
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildProfileCard(
    BuildContext context,
    Color surfaceColor,
    Color borderColor,
    Color textColor,
    Color subtextColor
  ) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: surfaceColor,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: borderColor),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 6,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Row(
        children: [
          Stack(
            children: [
              Container(
                width: 64,
                height: 64,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(color: Colors.white, width: 2),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.1),
                      blurRadius: 4,
                    ),
                  ],
                  image: const DecorationImage(
                    image: NetworkImage('https://lh3.googleusercontent.com/aida-public/AB6AXuA_N6zTnBIJCnQ4m57PnuQ3vepBXgE963RqsOtZ8U__dK-SeyyNEKhYXfT8xMZ_zbNcxVTCBAoiIA4CAhtAjj_NpOmqW8b5KLbQRV0epA2Ox2qy5hd0NR_9iE89TKdnZ50Lv9LGcuDUZu5S40EgHl7y6LI-rgA2yVPbmb__Y-RhTj9qK4CcBSsiOfFBfbh0VLn0F3Fl22CovkUemlxRYH3yovoEozDtSiKGQpoUIAkH3oXb7Tc9MpxohjfbfcvyWHCUWLjoOsa37zM'),
                    fit: BoxFit.cover,
                  ),
                ),
              ),
              Positioned(
                bottom: 0,
                right: 0,
                child: Container(
                  width: 16,
                  height: 16,
                  decoration: BoxDecoration(
                    color: ValoraColors.success,
                    shape: BoxShape.circle,
                    border: Border.all(color: surfaceColor, width: 2),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Sarah Jenkins',
                  style: TextStyle(
                    color: textColor,
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  'Premium Member',
                  style: TextStyle(
                    color: subtextColor,
                    fontSize: 14,
                  ),
                ),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            decoration: BoxDecoration(
              color: ValoraColors.primary.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(20),
            ),
            child: const Text(
              'Edit',
              style: TextStyle(
                color: ValoraColors.primary,
                fontWeight: FontWeight.w600,
                fontSize: 14,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSectionHeader(String title, Color color) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Padding(
        padding: const EdgeInsets.only(left: 4.0),
        child: Text(
          title,
          style: TextStyle(
            color: color,
            fontSize: 12,
            fontWeight: FontWeight.w600,
            letterSpacing: 1.0,
          ),
        ),
      ),
    );
  }

  Widget _buildSettingsTile(
    BuildContext context, {
    required IconData icon,
    required Color iconColor,
    required Color iconBgColor,
    required String title,
    required String subtitle,
    Color? subtitleColor,
    required bool showDivider,
    VoidCallback? onTap,
  }) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final textColor = isDark ? ValoraColors.onSurfaceDark : ValoraColors.onSurfaceLight;
    final subtextColor = subtitleColor ?? (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500);
    final dividerColor = isDark ? ValoraColors.neutral800 : ValoraColors.neutral100;

    return InkWell(
      onTap: onTap ?? () {},
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Row(
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: iconBgColor,
                    shape: BoxShape.circle,
                  ),
                  child: Icon(icon, color: iconColor, size: 20),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: TextStyle(
                          color: textColor,
                          fontSize: 16,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        subtitle,
                        style: TextStyle(
                          color: subtextColor,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
                Icon(
                  Icons.chevron_right_rounded,
                  color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral300,
                ),
              ],
            ),
          ),
          if (showDivider)
            Divider(height: 1, thickness: 1, color: dividerColor, indent: 72),
        ],
      ),
    );
  }
}
