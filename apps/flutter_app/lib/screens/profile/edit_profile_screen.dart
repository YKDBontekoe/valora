import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/user_profile_provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../widgets/common/valora_button.dart';
import '../../widgets/common/valora_text_field.dart';

class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key});

  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _firstNameController;
  late TextEditingController _lastNameController;

  @override
  void initState() {
    super.initState();
    final profile = context.read<UserProfileProvider>().profile;
    _firstNameController = TextEditingController(text: profile?.firstName);
    _lastNameController = TextEditingController(text: profile?.lastName);
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_formKey.currentState!.validate()) {
      final success = await context.read<UserProfileProvider>().updateProfile(
        firstName: _firstNameController.text.trim(),
        lastName: _lastNameController.text.trim(),
      );

      if (success && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Profile updated successfully')),
        );
        Navigator.pop(context);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final backgroundColor = isDark ? ValoraColors.backgroundDark : ValoraColors.backgroundLight;

    return Scaffold(
      backgroundColor: backgroundColor,
      appBar: AppBar(
        title: const Text('Edit Profile'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        foregroundColor: isDark ? Colors.white : Colors.black,
      ),
      body: Consumer<UserProfileProvider>(
        builder: (context, provider, _) {
          return SingleChildScrollView(
            padding: const EdgeInsets.all(24.0),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  ValoraTextField(
                    controller: _firstNameController,
                    label: 'First Name',
                    hint: 'Enter your first name',
                    validator: (value) =>
                        value == null || value.isEmpty ? 'Please enter your first name' : null,
                  ),
                  const SizedBox(height: 16),
                  ValoraTextField(
                    controller: _lastNameController,
                    label: 'Last Name',
                    hint: 'Enter your last name',
                    validator: (value) =>
                        value == null || value.isEmpty ? 'Please enter your last name' : null,
                  ),
                  const SizedBox(height: 32),
                  ValoraButton(
                    label: 'Save Changes',
                    onPressed: provider.isLoading ? null : _save,
                    isLoading: provider.isLoading,
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
            ),
          );
        },
      ),
    );
  }
}
