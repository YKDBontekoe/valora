import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import 'workspace_list_screen.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../providers/theme_provider.dart';
import '../providers/auth_provider.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  Future<void> _openExternal(BuildContext context, Uri uri) async {
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication) &&
        context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Unable to open link right now.'),
          backgroundColor: ValoraColors.error,
        ),
      );
    }
  }

  Future<void> _openSupportEmail(BuildContext context) async {
    final Uri supportUri = Uri(
      scheme: 'mailto',
      path: 'support@valora.nl',
      queryParameters: const <String, String>{
        'subject': 'Valora Support Request',
      },
    );
    await _openExternal(context, supportUri);
  }

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
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;
    final subtextColor = isDark
        ? ValoraColors.neutral400
        : ValoraColors.neutral500;

    return Scaffold(
      backgroundColor: colorScheme.surface,
      body: CustomScrollView(
        slivers: [
          // Sticky Header
          SliverAppBar(
            pinned: true,
            backgroundColor: colorScheme.surface.withValues(alpha: 0.95),
            surfaceTintColor: Colors.transparent,
            elevation: 0,
            automaticallyImplyLeading: false, // No back button since it's a tab
            title: Text(
              'Settings',
              style: ValoraTypography.headlineMedium.copyWith(
                color: colorScheme.onSurface,
                fontWeight: FontWeight.bold,
              ),
            ),
            actions: [
              Consumer<ThemeProvider>(
                builder: (context, themeProvider, _) {
                  final isDarkMode = themeProvider.isDarkMode;
                  return Padding(
                    padding: const EdgeInsets.only(right: ValoraSpacing.md),
                    child: IconButton(
                      onPressed: () {
                        themeProvider.toggleTheme();
                      },
                      icon: Icon(
                        isDarkMode
                            ? Icons.light_mode_rounded
                            : Icons.dark_mode_rounded,
                        color: subtextColor,
                      ),
                      style: IconButton.styleFrom(
                        backgroundColor: isDark
                            ? ValoraColors.neutral800
                            : ValoraColors.neutral100,
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
              padding: const EdgeInsets.symmetric(
                horizontal: ValoraSpacing.lg,
                vertical: ValoraSpacing.sm,
              ),
              child: Column(
                children: [
                  // Profile Section
                  _buildProfileCard(context),

                  const SizedBox(height: ValoraSpacing.xl),

                  // Preferences Section
                  _buildSectionHeader('PREFERENCES', subtextColor),
                  const SizedBox(height: ValoraSpacing.sm),
                  ValoraCard(
                    padding: EdgeInsets.zero,
                    child: Column(
                      children: [
                        ValoraSettingsTile(
                          icon: Icons.workspaces_rounded,
                          iconColor: Colors.blue,
                          iconBackgroundColor: Colors.blue.withValues(alpha: 0.1),
                          title: 'Workspaces',
                          subtitle: 'Collaborate on your property search',
                          showDivider: true,
                          onTap: () {
                            Navigator.push(
                              context,
                              MaterialPageRoute(
                                builder: (context) => ChangeNotifierProvider.value(
                                  value: context.read<WorkspaceProvider>(),
                                  child: const WorkspaceListScreen(),
                                ),
                              ),
                            );
                          },
                        ),
                        ValoraSettingsTile(
                          icon: Icons.tune_rounded,
                          iconColor: Colors.purple,
                          iconBackgroundColor: Colors.purple.withValues(alpha: 0.1),
                          title: 'Search Preferences',
                          subtitle: 'Location, Price, Amenities',
                          showDivider: true,
                          onTap: () => _openExternal(
                            context,
                            Uri.parse('https://valora.nl/preferences/search'),
                          ),
                        ),
                        ValoraSettingsTile(
                          icon: Icons.palette_rounded,
                          iconColor: Colors.orange,
                          iconBackgroundColor: Colors.orange.withValues(alpha: 0.1),
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

                  const SizedBox(height: ValoraSpacing.xl),

                  // Account & Security Section
                  _buildSectionHeader('ACCOUNT & SECURITY', subtextColor),
                  const SizedBox(height: ValoraSpacing.sm),
                  ValoraCard(
                    padding: EdgeInsets.zero,
                    child: Column(
                      children: [
                        ValoraSettingsTile(
                          icon: Icons.card_membership_rounded,
                          iconColor: ValoraColors.success,
                          iconBackgroundColor: ValoraColors.success.withValues(
                            alpha: 0.1,
                          ),
                          title: 'Subscription',
                          subtitle: 'Pro Plan Active',
                          showDivider: true,
                          onTap: () => _openExternal(
                            context,
                            Uri.parse('https://valora.nl/account/subscription'),
                          ),
                        ),
                        ValoraSettingsTile(
                          icon: Icons.lock_rounded,
                          iconColor: Colors.grey,
                          iconBackgroundColor: Colors.grey.withValues(alpha: 0.1),
                          title: 'Privacy & Security',
                          subtitle: 'Password, FaceID',
                          showDivider: false,
                          onTap: () => _openExternal(
                            context,
                            Uri.parse('https://valora.nl/privacy'),
                          ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: ValoraSpacing.xl),

                  // Help Section
                  ValoraCard(
                    backgroundColor: ValoraColors.primary.withValues(
                      alpha: isDark ? 0.1 : 0.05,
                    ),
                    borderColor: ValoraColors.primary.withValues(alpha: 0.2),
                    padding: const EdgeInsets.all(ValoraSpacing.md),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Need Help?',
                              style: ValoraTypography.titleMedium.copyWith(
                                color: isDark
                                    ? ValoraColors.primaryLight
                                    : ValoraColors.primary,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(height: ValoraSpacing.xs),
                            Text(
                              'Our support team is available 24/7',
                              style: ValoraTypography.bodySmall.copyWith(
                                color: subtextColor,
                              ),
                            ),
                          ],
                        ),
                        ValoraButton(
                          label: 'Contact Us',
                          variant: ValoraButtonVariant.outline,
                          size: ValoraButtonSize.small,
                          onPressed: () => _openSupportEmail(context),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: ValoraSpacing.xl),

                  // Logout
                  ValoraButton(
                    label: 'Log Out',
                    icon: Icons.logout_rounded,
                    variant: ValoraButtonVariant.ghost,
                    isFullWidth: true,
                    onPressed: () => _confirmLogout(context),
                  ),

                  const SizedBox(height: ValoraSpacing.md),

                  Text(
                    'Valora v2.4.0 (Build 392)',
                    style: ValoraTypography.labelSmall.copyWith(
                      color: subtextColor.withValues(alpha: 0.5),
                    ),
                  ),

                  const SizedBox(
                    height: 100,
                  ), // Bottom padding for navigation bar
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildProfileCard(BuildContext context) {
    // Get user info from AuthProvider
    final authProvider = context.watch<AuthProvider>();
    final userEmail = authProvider.email ?? 'Unknown user';
    final initials = userEmail.trim().isNotEmpty
        ? userEmail.substring(0, userEmail.length >= 2 ? 2 : 1).toUpperCase()
        : 'U';

    final colorScheme = Theme.of(context).colorScheme;
    final textColor = colorScheme.onSurface;
    final subtextColor = colorScheme.onSurfaceVariant;

    return ValoraCard(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      child: Row(
        children: [
          Stack(
            children: [
              Container(
                width: 64,
                height: 64,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: ValoraColors.primary.withValues(alpha: 0.12),
                  border: Border.all(color: Colors.white, width: 2),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.1),
                      blurRadius: 4,
                    ),
                  ],
                ),
                child: Center(
                  child: Text(
                    initials,
                    style: ValoraTypography.titleLarge.copyWith(
                      color: ValoraColors.primary,
                      fontWeight: FontWeight.bold,
                    ),
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
                    border: Border.all(color: colorScheme.surface, width: 2),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(width: ValoraSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  userEmail,
                  style: ValoraTypography.titleMedium.copyWith(
                    color: textColor,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  'Premium Member',
                  style: ValoraTypography.bodyMedium.copyWith(
                    color: subtextColor,
                  ),
                ),
              ],
            ),
          ),
          ValoraChip(
            label: 'Edit',
            isSelected: true,
            onSelected: (_) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Profile editing coming soon!'),
                  behavior: SnackBarBehavior.floating,
                ),
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildSectionHeader(String title, Color color) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Padding(
        padding: const EdgeInsets.only(left: ValoraSpacing.xs),
        child: Text(
          title,
          style: ValoraTypography.labelSmall.copyWith(
            color: color,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.0,
          ),
        ),
      ),
    );
  }
}
