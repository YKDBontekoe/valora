import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';

class PrivacySecurityScreen extends StatelessWidget {
  const PrivacySecurityScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final backgroundColor = isDark
        ? ValoraColors.backgroundDark
        : ValoraColors.backgroundLight;
    final textColor = isDark
        ? ValoraColors.onBackgroundDark
        : ValoraColors.onBackgroundLight;
    final subtextColor = isDark
        ? ValoraColors.neutral400
        : ValoraColors.neutral500;

    return Scaffold(
      backgroundColor: backgroundColor,
      appBar: AppBar(
        title: Text(
          'Privacy & Security',
          style: TextStyle(color: textColor, fontWeight: FontWeight.bold),
        ),
        backgroundColor: backgroundColor,
        elevation: 0,
        iconTheme: IconThemeData(color: textColor),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildSection(
              'Data Protection',
              'Valora takes your privacy seriously. All search data is stored locally on your device unless you choose to sync it with your account. We do not sell your personal data to third parties.',
              textColor,
              subtextColor,
            ),
            const SizedBox(height: 24),
            _buildSection(
              'Location Services',
              'Location access is used solely to provide relevant listing results and context reports. You can revoke this permission at any time in your device settings.',
              textColor,
              subtextColor,
            ),
            const SizedBox(height: 24),
            _buildSection(
              'Secure Authentication',
              'We use industry-standard encryption for all authentication tokens. Your password is never stored in plain text.',
              textColor,
              subtextColor,
            ),
            const SizedBox(height: 32),
            Center(
              child: Text(
                'Valora Privacy Policy v1.2',
                style: TextStyle(
                  color: subtextColor,
                  fontSize: 12,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSection(
    String title,
    String content,
    Color titleColor,
    Color contentColor,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: TextStyle(
            color: titleColor,
            fontSize: 18,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          content,
          style: TextStyle(
            color: contentColor,
            fontSize: 14,
            height: 1.5,
          ),
        ),
      ],
    );
  }
}
