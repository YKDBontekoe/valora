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
import '../providers/settings_provider.dart';
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

  Future<void> _confirmClearData(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Clear All Data?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Clear Data',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text(
          'This will clear your search history, local settings, and log you out. This action cannot be undone.',
        ),
      ),
    );

    if (confirmed == true && context.mounted) {
      await context.read<SettingsProvider>().clearAllData(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;
    final subtextColor = isDark ? ValoraColors.neutral400 : ValoraColors.neutral500;

    return Scaffold(
      backgroundColor: colorScheme.surface,
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            pinned: true,
            backgroundColor: colorScheme.surface.withValues(alpha: 0.95),
            surfaceTintColor: Colors.transparent,
            elevation: 0,
            automaticallyImplyLeading: false,
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
                      onPressed: () => themeProvider.toggleTheme(),
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
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: ValoraSpacing.lg,
                vertical: ValoraSpacing.sm,
              ),
              child: Column(
                children: [
                  _buildProfileCard(context),
                  const SizedBox(height: ValoraSpacing.xl),

                  // MAP & REPORTS SECTION
                  const ValoraSectionHeader(title: 'MAP & REPORTS'),
                  Consumer<SettingsProvider>(
                    builder: (context, settings, _) {
                      return ValoraCard(
                        padding: const EdgeInsets.symmetric(vertical: ValoraSpacing.sm),
                        child: Column(
                          children: [
                            Padding(
                              padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md, vertical: ValoraSpacing.xs),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text('Report Radius', style: ValoraTypography.bodyLarge),
                                  Text('${settings.reportRadius.toInt()}m', style: ValoraTypography.bodyMedium.copyWith(color: subtextColor)),
                                ],
                              ),
                            ),
                            Slider(
                              value: settings.reportRadius,
                              min: 100,
                              max: 2000,
                              divisions: 19,
                              label: '${settings.reportRadius.toInt()}m',
                              onChanged: (value) => settings.setReportRadius(value),
                              onChangeEnd: (value) => settings.persistReportRadius(),
                            ),
                            const Divider(height: 1, indent: 16, endIndent: 16),
                            Padding(
                              padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md, vertical: ValoraSpacing.sm),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text('Default Metric', style: ValoraTypography.bodyLarge),
                                  DropdownButton<String>(
                                    value: settings.mapDefaultMetric,
                                    underline: const SizedBox(),
                                    items: const [
                                      DropdownMenuItem(value: 'price', child: Text('Price')),
                                      DropdownMenuItem(value: 'size', child: Text('Size')),
                                      DropdownMenuItem(value: 'year', child: Text('Year')),
                                    ],
                                    onChanged: (value) {
                                      if (value != null) settings.setMapDefaultMetric(value);
                                    },
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
                  const SizedBox(height: ValoraSpacing.xl),

                  // PREFERENCES SECTION
                  const ValoraSectionHeader(title: 'PREFERENCES'),
                  Consumer<SettingsProvider>(
                    builder: (context, settings, _) {
                      return ValoraCard(
                        padding: EdgeInsets.zero,
                        child: Column(
                          children: [
                            ValoraSettingsTile(
                              icon: Icons.workspaces_rounded,
                              iconColor: ValoraColors.info,
                              iconBackgroundColor: ValoraColors.info.withValues(alpha: 0.1),
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
                            SwitchListTile(
                              title: Text('Notifications', style: ValoraTypography.bodyLarge.copyWith(fontWeight: FontWeight.w600)),
                              subtitle: Text('Enable push notifications', style: ValoraTypography.bodySmall.copyWith(color: subtextColor)),
                              value: settings.notificationsEnabled,
                              onChanged: (value) => settings.setNotificationsEnabled(value),
                              secondary: Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: Colors.red.withValues(alpha: 0.1),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: const Icon(Icons.notifications_rounded, color: Colors.red),
                              ),
                              contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                            ),
                            if (settings.notificationsEnabled) ...[
                              const Divider(height: 1, indent: 64, endIndent: 16),
                              Padding(
                                padding: const EdgeInsets.only(left: 72, right: 16, top: 8, bottom: 8),
                                child: Row(
                                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                  children: [
                                    Text('Frequency', style: ValoraTypography.bodyMedium),
                                    DropdownButton<String>(
                                      value: settings.notificationFrequency,
                                      underline: const SizedBox(),
                                      style: ValoraTypography.bodyMedium.copyWith(color: colorScheme.onSurface),
                                      items: const [
                                        DropdownMenuItem(value: 'realtime', child: Text('Real-time')),
                                        DropdownMenuItem(value: 'daily', child: Text('Daily Digest')),
                                        DropdownMenuItem(value: 'weekly', child: Text('Weekly')),
                                      ],
                                      onChanged: (value) {
                                        if (value != null) settings.setNotificationFrequency(value);
                                      },
                                    ),
                                  ],
                                ),
                              ),
                            ],
                            const Divider(height: 1, indent: 16, endIndent: 16),
                            ValoraSettingsTile(
                              icon: Icons.tune_rounded,
                              iconColor: ValoraColors.primary,
                              iconBackgroundColor: ValoraColors.primary.withValues(alpha: 0.1),
                              title: 'Search Preferences',
                              subtitle: 'Location, Price, Amenities',
                              showDivider: true,
                              onTap: () => _openExternal(context, Uri.parse('https://valora.nl/preferences/search')),
                            ),
                            ValoraSettingsTile(
                              icon: Icons.palette_rounded,
                              iconColor: ValoraColors.accent,
                              iconBackgroundColor: ValoraColors.accent.withValues(alpha: 0.1),
                              title: 'Appearance',
                              subtitle: 'Theme & Display settings',
                              showDivider: false,
                              onTap: () => context.read<ThemeProvider>().toggleTheme(),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
                  const SizedBox(height: ValoraSpacing.xl),

                  // ACCOUNT & SECURITY / DATA SECTION
                  const ValoraSectionHeader(title: 'ACCOUNT & SECURITY'),
                  ValoraCard(
                    padding: const EdgeInsets.all(ValoraSpacing.md),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        ValoraSettingsTile(
                          icon: Icons.lock_rounded,
                          iconColor: ValoraColors.neutral500,
                          iconBackgroundColor: ValoraColors.neutral500.withValues(alpha: 0.1),
                          title: 'Privacy & Security',
                          subtitle: 'Password, FaceID',
                          showDivider: true,
                          onTap: () => _openExternal(context, Uri.parse('https://valora.nl/privacy')),
                        ),
                        const SizedBox(height: ValoraSpacing.md),
                        Text('Data Management', style: ValoraTypography.titleMedium.copyWith(fontWeight: FontWeight.bold)),
                        const SizedBox(height: ValoraSpacing.xs),
                        Text(
                          'We cache some data locally to improve performance. You can clear this data at any time.',
                          style: ValoraTypography.bodySmall.copyWith(color: subtextColor),
                        ),
                        const SizedBox(height: ValoraSpacing.md),
                        SizedBox(
                          width: double.infinity,
                          child: ValoraButton(
                            label: 'Clear Cache & History',
                            icon: Icons.delete_outline_rounded,
                            variant: ValoraButtonVariant.outline,
                            onPressed: () => _confirmClearData(context),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: ValoraSpacing.xl),

                  // DIAGNOSTICS
                  Consumer<SettingsProvider>(
                    builder: (context, settings, _) {
                      return ValoraCard(
                        padding: EdgeInsets.zero,
                        child: SwitchListTile(
                          title: Text('Diagnostics', style: ValoraTypography.bodyLarge.copyWith(fontWeight: FontWeight.w600)),
                          subtitle: Text('Help support troubleshoot issues', style: ValoraTypography.bodySmall.copyWith(color: subtextColor)),
                          value: settings.diagnosticsEnabled,
                          onChanged: (value) => settings.setDiagnosticsEnabled(value),
                          secondary: Container(
                            padding: const EdgeInsets.all(8),
                            decoration: BoxDecoration(
                              color: Colors.teal.withValues(alpha: 0.1),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: const Icon(Icons.bug_report_rounded, color: Colors.teal),
                          ),
                        ),
                      );
                    },
                  ),
                  const SizedBox(height: ValoraSpacing.xl),

                  // HELP SECTION
                  ValoraCard(
                    backgroundColor: ValoraColors.primary.withValues(alpha: isDark ? 0.1 : 0.05),
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
                                color: isDark ? ValoraColors.primaryLight : ValoraColors.primary,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(height: ValoraSpacing.xs),
                            Text('Our support team is available 24/7', style: ValoraTypography.bodySmall.copyWith(color: subtextColor)),
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

                  ValoraButton(
                    label: 'Log Out',
                    icon: Icons.logout_rounded,
                    variant: ValoraButtonVariant.ghost,
                    isFullWidth: true,
                    onPressed: () => _confirmLogout(context),
                  ),
                  const SizedBox(height: ValoraSpacing.md),

                  Consumer<SettingsProvider>(
                    builder: (context, settings, _) {
                      return Text(
                        'Valora v${settings.appVersion} (Build ${settings.buildNumber})',
                        style: ValoraTypography.labelSmall.copyWith(color: subtextColor.withValues(alpha: 0.5)),
                      );
                    },
                  ),
                  const SizedBox(height: 100),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildProfileCard(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();
    final userEmail = authProvider.email ?? 'Unknown user';
    final initials = userEmail.trim().isNotEmpty ? userEmail.substring(0, userEmail.length >= 2 ? 2 : 1).toUpperCase() : 'U';
    final colorScheme = Theme.of(context).colorScheme;

    return ValoraCard(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      child: Row(
        children: [
          ValoraAvatar(initials: initials, size: ValoraAvatarSize.large, showOnlineIndicator: true),
          const SizedBox(width: ValoraSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(userEmail, style: ValoraTypography.titleMedium.copyWith(color: colorScheme.onSurface, fontWeight: FontWeight.bold)),
                const SizedBox(height: 2),
                Text('Premium Member', style: ValoraTypography.bodyMedium.copyWith(color: colorScheme.onSurfaceVariant)),
              ],
            ),
          ),
          ValoraChip(
            label: 'Edit',
            isSelected: true,
            onSelected: (_) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Profile editing coming soon!'), behavior: SnackBarBehavior.floating),
              );
            },
          ),
        ],
      ),
    );
  }
}