import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../providers/preferences_provider.dart';

class NotificationSettingsScreen extends StatelessWidget {
  const NotificationSettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final backgroundColor = isDark
        ? ValoraColors.backgroundDark
        : ValoraColors.backgroundLight;
    final textColor = isDark
        ? ValoraColors.onBackgroundDark
        : ValoraColors.onBackgroundLight;

    return Scaffold(
      backgroundColor: backgroundColor,
      appBar: AppBar(
        title: Text(
          'Notifications',
          style: TextStyle(color: textColor, fontWeight: FontWeight.bold),
        ),
        backgroundColor: backgroundColor,
        elevation: 0,
        iconTheme: IconThemeData(color: textColor),
      ),
      body: Consumer<PreferencesProvider>(
        builder: (context, provider, _) {
          return ListView(
            padding: const EdgeInsets.all(16.0),
            children: [
              SwitchListTile(
                title: Text(
                  'Enable Notifications',
                  style: TextStyle(
                    color: textColor,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                subtitle: Text(
                  'Receive alerts about saved searches and price drops',
                  style: TextStyle(
                    color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                  ),
                ),
                value: provider.notificationsEnabled,
                onChanged: (value) {
                  provider.setNotificationsEnabled(value);
                },
                activeColor: ValoraColors.primary,
              ),
              const Divider(),
              if (provider.notificationsEnabled) ...[
                const SizedBox(height: 16),
                _buildDisabledOption(context, 'Price Drop Alerts'),
                _buildDisabledOption(context, 'New Listing Alerts'),
                _buildDisabledOption(context, 'Open House Reminders'),
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: ValoraColors.primary.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(
                      color: ValoraColors.primary.withOpacity(0.3),
                    ),
                  ),
                  child: Row(
                    children: [
                      const Icon(
                        Icons.info_outline_rounded,
                        color: ValoraColors.primary,
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          'Granular notification settings will be available in a future update.',
                          style: TextStyle(
                            color: isDark
                                ? ValoraColors.neutral200
                                : ValoraColors.neutral700,
                            fontSize: 12,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          );
        },
      ),
    );
  }

  Widget _buildDisabledOption(BuildContext context, String title) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final textColor = isDark
        ? ValoraColors.neutral400
        : ValoraColors.neutral500;

    return ListTile(
      enabled: false,
      title: Text(title, style: TextStyle(color: textColor)),
      trailing: Switch(
        value: true,
        onChanged: null,
      ),
    );
  }
}
