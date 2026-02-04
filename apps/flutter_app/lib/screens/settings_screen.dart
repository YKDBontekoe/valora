import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../providers/theme_provider.dart';
import '../providers/auth_provider.dart';
import '../widgets/valora_widgets.dart';
import '../services/api_service.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  Future<void> _confirmLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Log Out?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Log Out',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text('Are you sure you want to log out?'),
      ),
    );

    if (confirmed == true && context.mounted) {
      context.read<AuthProvider>().logout();
    }
  }

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

                  _buildAdminSection(context, surfaceColor, borderColor, subtextColor),
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
                    onPressed: () => _confirmLogout(context),
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

  Widget _buildAdminSection(BuildContext context, Color surfaceColor, Color borderColor, Color subtextColor) {
    final isAdmin = context.watch<AuthProvider>().isAdmin;
    if (!isAdmin) return const SizedBox.shrink();

    return Column(
      children: [
        _buildSectionHeader('ADMIN CONTROLS', subtextColor),
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
                icon: Icons.refresh_rounded,
                iconColor: Colors.blue,
                iconBgColor: Colors.blue.withValues(alpha: 0.1),
                title: 'Trigger Scrape',
                subtitle: 'Start full scraping job',
                showDivider: true,
                onTap: () => _triggerScrape(context),
              ),
              _buildSettingsTile(
                context,
                icon: Icons.filter_list_rounded,
                iconColor: Colors.orange,
                iconBgColor: Colors.orange.withValues(alpha: 0.1),
                title: 'Limited Scrape',
                subtitle: 'Scrape specific region/limit',
                showDivider: true,
                onTap: () => _triggerLimitedScrapeDialog(context),
              ),
              _buildSettingsTile(
                context,
                icon: Icons.cloud_download_rounded,
                iconColor: Colors.green,
                iconBgColor: Colors.green.withValues(alpha: 0.1),
                title: 'Seed Database',
                subtitle: 'Initial seed for region',
                showDivider: true,
                onTap: () => _seedDatabaseDialog(context),
              ),
              _buildSettingsTile(
                context,
                icon: Icons.delete_forever_rounded,
                iconColor: Colors.red,
                iconBgColor: Colors.red.withValues(alpha: 0.1),
                title: 'Clear Database',
                subtitle: 'Remove all listings',
                subtitleColor: Colors.red,
                showDivider: false,
                onTap: () => _confirmClearDatabase(context),
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),
      ],
    );
  }

  Future<void> _triggerScrape(BuildContext context) async {
    try {
      await context.read<ApiService>().triggerScrape();
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Scrape job triggered successfully')),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }

  Future<void> _triggerLimitedScrapeDialog(BuildContext context) async {
    final regionController = TextEditingController();
    final limitController = TextEditingController(text: '10');

    final result = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Limited Scrape',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Trigger',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: regionController,
              decoration: const InputDecoration(labelText: 'Region (e.g. amsterdam)'),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: limitController,
              decoration: const InputDecoration(labelText: 'Limit'),
              keyboardType: TextInputType.number,
            ),
          ],
        ),
      ),
    );

    if (result == true && context.mounted) {
      try {
        final region = regionController.text;
        final limit = int.tryParse(limitController.text) ?? 10;
        if (region.isEmpty) return;

        await context.read<ApiService>().triggerLimitedScrape(region, limit);
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Limited scrape triggered')),
          );
        }
      } catch (e) {
         if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
          );
        }
      }
    }
  }

  Future<void> _seedDatabaseDialog(BuildContext context) async {
      final regionController = TextEditingController();

      final result = await showDialog<bool>(
        context: context,
        builder: (context) => ValoraDialog(
          title: 'Seed Database',
          actions: [
            ValoraButton(
              label: 'Cancel',
              variant: ValoraButtonVariant.ghost,
              onPressed: () => Navigator.pop(context, false),
            ),
            ValoraButton(
              label: 'Seed',
              variant: ValoraButtonVariant.primary,
              onPressed: () => Navigator.pop(context, true),
            ),
          ],
          child: TextField(
            controller: regionController,
            decoration: const InputDecoration(labelText: 'Region (e.g. amsterdam)'),
          ),
        ),
      );

    if (result == true && context.mounted) {
      try {
        final region = regionController.text;
        if (region.isEmpty) return;

        await context.read<ApiService>().seedDatabase(region);
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Seed job triggered')),
          );
        }
      } catch (e) {
         if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
          );
        }
      }
    }
  }

  Future<void> _confirmClearDatabase(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Clear Database?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Clear All',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text('This will permanently delete all listings from the database. This action cannot be undone.', style: TextStyle(color: Colors.red)),
      ),
    );

    if (confirmed == true && context.mounted) {
       try {
        await context.read<ApiService>().clearDatabase();
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Database cleared successfully')),
          );
        }
      } catch (e) {
         if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
          );
        }
      }
    }
  }

  Widget _buildProfileCard(
    BuildContext context,
    Color surfaceColor,
    Color borderColor,
    Color textColor,
    Color subtextColor
  ) {
    // Get user info from AuthProvider
    final authProvider = context.watch<AuthProvider>();
    final userEmail = authProvider.email ?? 'Sarah Jenkins';

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
                  userEmail,
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
