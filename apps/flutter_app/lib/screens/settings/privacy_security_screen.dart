import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/user_profile_provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../widgets/common/valora_button.dart';
import '../../widgets/common/valora_text_field.dart';

class PrivacySecurityScreen extends StatefulWidget {
  const PrivacySecurityScreen({super.key});

  @override
  State<PrivacySecurityScreen> createState() => _PrivacySecurityScreenState();
}

class _PrivacySecurityScreenState extends State<PrivacySecurityScreen> {
  final _formKey = GlobalKey<FormState>();
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  bool _biometricsSupported = false;

  @override
  void initState() {
    super.initState();
    _checkBiometrics();
  }

  Future<void> _checkBiometrics() async {
    final supported = await context.read<UserProfileProvider>().checkBiometrics();
    if (mounted) {
      setState(() {
        _biometricsSupported = supported;
      });
    }
  }

  @override
  void dispose() {
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _changePassword() async {
    if (_formKey.currentState!.validate()) {
      final success = await context.read<UserProfileProvider>().changePassword(
        _currentPasswordController.text,
        _newPasswordController.text,
        _confirmPasswordController.text,
      );

      if (success && mounted) {
        _currentPasswordController.clear();
        _newPasswordController.clear();
        _confirmPasswordController.clear();
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Password changed successfully')),
        );
      }
    }
  }

  Future<void> _toggleBiometrics(bool value) async {
    final provider = context.read<UserProfileProvider>();
    if (value) {
      final authenticated = await provider.authenticate();
      if (!authenticated) return;
    }

    await provider.updateProfile(biometricsEnabled: value);
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final backgroundColor = isDark ? ValoraColors.backgroundDark : ValoraColors.backgroundLight;
    final textColor = isDark ? Colors.white : Colors.black;

    return Scaffold(
      backgroundColor: backgroundColor,
      appBar: AppBar(
        title: const Text('Privacy & Security'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        foregroundColor: textColor,
      ),
      body: Consumer<UserProfileProvider>(
        builder: (context, provider, _) {
          return SingleChildScrollView(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (_biometricsSupported) ...[
                  Text(
                    'Biometrics',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: textColor),
                  ),
                  const SizedBox(height: 16),
                  SwitchListPlatform(
                    title: 'Use FaceID / Fingerprint',
                    subtitle: 'Secure your account with biometrics',
                    value: provider.profile?.biometricsEnabled ?? false,
                    onChanged: _toggleBiometrics,
                  ),
                  const Divider(height: 48),
                ],
                Text(
                  'Change Password',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: textColor),
                ),
                const SizedBox(height: 16),
                Form(
                  key: _formKey,
                  child: Column(
                    children: [
                      ValoraTextField(
                        controller: _currentPasswordController,
                        label: 'Current Password',
                        obscureText: true,
                        validator: (value) =>
                            value == null || value.isEmpty ? 'Current password is required' : null,
                      ),
                      const SizedBox(height: 16),
                      ValoraTextField(
                        controller: _newPasswordController,
                        label: 'New Password',
                        obscureText: true,
                        validator: (value) {
                          if (value == null || value.isEmpty) return 'New password is required';
                          if (value.length < 8) return 'Password must be at least 8 characters';
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      ValoraTextField(
                        controller: _confirmPasswordController,
                        label: 'Confirm New Password',
                        obscureText: true,
                        validator: (value) {
                          if (value != _newPasswordController.text) return 'Passwords do not match';
                          return null;
                        },
                      ),
                      const SizedBox(height: 32),
                      ValoraButton(
                        label: 'Update Password',
                        onPressed: provider.isLoading ? null : _changePassword,
                        isLoading: provider.isLoading,
                      ),
                    ],
                  ),
                ),
                if (provider.error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 16.0),
                    child: Text(
                      provider.error!,
                      style: const TextStyle(color: ValoraColors.error),
                      textAlign: TextAlign.center,
                    ),
                  ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class SwitchListPlatform extends StatelessWidget {
  final String title;
  final String subtitle;
  final bool value;
  final ValueChanged<bool> onChanged;

  const SwitchListPlatform({
    super.key,
    required this.title,
    required this.subtitle,
    required this.value,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: TextStyle(fontWeight: FontWeight.w600, color: isDark ? Colors.white : Colors.black)),
              Text(subtitle, style: TextStyle(fontSize: 12, color: Colors.grey)),
            ],
          ),
        ),
        Switch.adaptive(
          value: value,
          onChanged: onChanged,
          activeTrackColor: ValoraColors.primary,
        ),
      ],
    );
  }
}
