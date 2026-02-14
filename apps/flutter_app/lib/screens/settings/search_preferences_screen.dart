import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/user_profile_provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../widgets/common/valora_button.dart';

class SearchPreferencesScreen extends StatefulWidget {
  const SearchPreferencesScreen({super.key});

  @override
  State<SearchPreferencesScreen> createState() => _SearchPreferencesScreenState();
}

class _SearchPreferencesScreenState extends State<SearchPreferencesScreen> {
  int _radius = 1000;

  @override
  void initState() {
    super.initState();
    _radius = context.read<UserProfileProvider>().profile?.defaultRadiusMeters ?? 1000;
  }

  Future<void> _save() async {
    final success = await context.read<UserProfileProvider>().updateProfile(
      defaultRadiusMeters: _radius,
    );

    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Search preferences updated')),
      );
      Navigator.pop(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final backgroundColor = isDark ? ValoraColors.backgroundDark : ValoraColors.backgroundLight;
    final surfaceColor = isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight;
    final textColor = isDark ? Colors.white : Colors.black;

    return Scaffold(
      backgroundColor: backgroundColor,
      appBar: AppBar(
        title: const Text('Search Preferences'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        foregroundColor: textColor,
      ),
      body: Consumer<UserProfileProvider>(
        builder: (context, provider, _) {
          return Padding(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Context Report Radius',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: textColor,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Choose the default radius for neighborhood analysis when searching for properties.',
                  style: TextStyle(color: isDark ? Colors.grey[400] : Colors.grey[600]),
                ),
                const SizedBox(height: 24),
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: surfaceColor,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: isDark ? Colors.grey[800]! : Colors.grey[200]!),
                  ),
                  child: Column(
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text('${_radius}m', style: TextStyle(fontWeight: FontWeight.bold, color: textColor)),
                          const Text('Max 5000m', style: TextStyle(color: Colors.grey, fontSize: 12)),
                        ],
                      ),
                      Slider(
                        value: _radius.toDouble(),
                        min: 100,
                        max: 5000,
                        divisions: 49,
                        activeColor: ValoraColors.primary,
                        onChanged: (value) {
                          setState(() {
                            _radius = value.round();
                          });
                        },
                      ),
                    ],
                  ),
                ),
                const Spacer(),
                ValoraButton(
                  label: 'Save Preferences',
                  onPressed: provider.isLoading ? null : _save,
                  isLoading: provider.isLoading,
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
