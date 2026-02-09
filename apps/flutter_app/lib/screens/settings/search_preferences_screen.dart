import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../providers/preferences_provider.dart';
import '../../widgets/valora_widgets.dart';

class SearchPreferencesScreen extends StatefulWidget {
  const SearchPreferencesScreen({super.key});

  @override
  State<SearchPreferencesScreen> createState() => _SearchPreferencesScreenState();
}

class _SearchPreferencesScreenState extends State<SearchPreferencesScreen> {
  late TextEditingController _cityController;
  late TextEditingController _minPriceController;
  late TextEditingController _maxPriceController;
  late TextEditingController _minBedroomsController;

  @override
  void initState() {
    super.initState();
    final provider = context.read<PreferencesProvider>();
    _cityController = TextEditingController(text: provider.defaultCity ?? '');
    _minPriceController = TextEditingController(
      text: provider.defaultMinPrice?.toStringAsFixed(0) ?? '',
    );
    _maxPriceController = TextEditingController(
      text: provider.defaultMaxPrice?.toStringAsFixed(0) ?? '',
    );
    _minBedroomsController = TextEditingController(
      text: provider.defaultMinBedrooms?.toString() ?? '',
    );
  }

  @override
  void dispose() {
    _cityController.dispose();
    _minPriceController.dispose();
    _maxPriceController.dispose();
    _minBedroomsController.dispose();
    super.dispose();
  }

  void _save(BuildContext context) {
    final provider = context.read<PreferencesProvider>();
    final city = _cityController.text.trim().isEmpty ? null : _cityController.text.trim();
    final minPrice = double.tryParse(_minPriceController.text);
    final maxPrice = double.tryParse(_maxPriceController.text);
    final minBedrooms = int.tryParse(_minBedroomsController.text);

    if (minPrice != null && maxPrice != null && minPrice > maxPrice) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Min price cannot be greater than Max price'),
          backgroundColor: ValoraColors.error,
        ),
      );
      return;
    }

    provider.setDefaultCity(city);
    provider.setDefaultMinPrice(minPrice);
    provider.setDefaultMaxPrice(maxPrice);
    provider.setDefaultMinBedrooms(minBedrooms);

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Preferences saved'),
        backgroundColor: ValoraColors.success,
      ),
    );
    Navigator.pop(context);
  }

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
          'Search Preferences',
          style: TextStyle(color: textColor, fontWeight: FontWeight.bold),
        ),
        backgroundColor: backgroundColor,
        elevation: 0,
        iconTheme: IconThemeData(color: textColor),
        actions: [
          TextButton(
            onPressed: () => _save(context),
            child: const Text('Save', style: TextStyle(fontWeight: FontWeight.bold)),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Set your default search criteria. These filters will be applied automatically when you start a new search.',
              style: TextStyle(
                color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                fontSize: 14,
              ),
            ),
            const SizedBox(height: 24),
            Text('Default Location', style: ValoraTypography.titleMedium),
            const SizedBox(height: ValoraSpacing.sm),
            ValoraTextField(
              controller: _cityController,
              label: 'City',
              hint: 'e.g. Amsterdam',
              prefixIcon: Icons.location_on_outlined,
            ),
            const SizedBox(height: ValoraSpacing.lg),
            Text('Price Range', style: ValoraTypography.titleMedium),
            const SizedBox(height: ValoraSpacing.sm),
            Row(
              children: [
                Expanded(
                  child: ValoraTextField(
                    controller: _minPriceController,
                    label: 'Min Price',
                    prefixText: '€ ',
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  ),
                ),
                const SizedBox(width: ValoraSpacing.md),
                Expanded(
                  child: ValoraTextField(
                    controller: _maxPriceController,
                    label: 'Max Price',
                    prefixText: '€ ',
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  ),
                ),
              ],
            ),
            const SizedBox(height: ValoraSpacing.lg),
            Text('Property Details', style: ValoraTypography.titleMedium),
            const SizedBox(height: ValoraSpacing.sm),
            ValoraTextField(
              controller: _minBedroomsController,
              label: 'Min Bedrooms',
              keyboardType: TextInputType.number,
              prefixIcon: Icons.bed_outlined,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            ),
          ],
        ),
      ),
    );
  }
}
