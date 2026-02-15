import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import 'valora_widgets.dart';

class SavedListingsFilterDialog extends StatefulWidget {
  final double? initialMinPrice;
  final double? initialMaxPrice;
  final String? initialCity;
  final int? initialMinBedrooms;
  final int? initialMinLivingArea;
  final int? initialMaxLivingArea;
  final double? initialMinSafetyScore;
  final double? initialMinCompositeScore;

  const SavedListingsFilterDialog({
    super.key,
    this.initialMinPrice,
    this.initialMaxPrice,
    this.initialCity,
    this.initialMinBedrooms,
    this.initialMinLivingArea,
    this.initialMaxLivingArea,
    this.initialMinSafetyScore,
    this.initialMinCompositeScore,
  });

  @override
  State<SavedListingsFilterDialog> createState() =>
      _SavedListingsFilterDialogState();
}

class _SavedListingsFilterDialogState extends State<SavedListingsFilterDialog> {
  late TextEditingController _minPriceController;
  late TextEditingController _maxPriceController;
  late TextEditingController _cityController;
  late TextEditingController _minBedroomsController;
  late TextEditingController _minLivingAreaController;
  late TextEditingController _maxLivingAreaController;
  late TextEditingController _minCompositeScoreController;
  late TextEditingController _minSafetyScoreController;

  @override
  void initState() {
    super.initState();
    _minPriceController = TextEditingController(
      text: widget.initialMinPrice?.toStringAsFixed(0) ?? '',
    );
    _maxPriceController = TextEditingController(
      text: widget.initialMaxPrice?.toStringAsFixed(0) ?? '',
    );
    _cityController = TextEditingController(text: widget.initialCity ?? '');
    _minBedroomsController = TextEditingController(
      text: widget.initialMinBedrooms?.toString() ?? '',
    );
    _minLivingAreaController = TextEditingController(
      text: widget.initialMinLivingArea?.toString() ?? '',
    );
    _maxLivingAreaController = TextEditingController(
      text: widget.initialMaxLivingArea?.toString() ?? '',
    );
    _minCompositeScoreController = TextEditingController(
      text: widget.initialMinCompositeScore?.toString() ?? '',
    );
    _minSafetyScoreController = TextEditingController(
      text: widget.initialMinSafetyScore?.toString() ?? '',
    );
  }

  @override
  void dispose() {
    _minPriceController.dispose();
    _maxPriceController.dispose();
    _cityController.dispose();
    _minBedroomsController.dispose();
    _minLivingAreaController.dispose();
    _maxLivingAreaController.dispose();
    _minCompositeScoreController.dispose();
    _minSafetyScoreController.dispose();
    super.dispose();
  }

  void _apply() {
    final minPrice = double.tryParse(_minPriceController.text);
    final maxPrice = double.tryParse(_maxPriceController.text);
    final city = _cityController.text.trim().isEmpty
        ? null
        : _cityController.text.trim();
    final minBedrooms = int.tryParse(_minBedroomsController.text);
    final minLivingArea = int.tryParse(_minLivingAreaController.text);
    final maxLivingArea = int.tryParse(_maxLivingAreaController.text);
    final minCompositeScore = double.tryParse(
      _minCompositeScoreController.text,
    );
    final minSafetyScore = double.tryParse(_minSafetyScoreController.text);

    if ((minCompositeScore != null &&
            (minCompositeScore < 0 || minCompositeScore > 100)) ||
        (minSafetyScore != null &&
            (minSafetyScore < 0 || minSafetyScore > 100))) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Score must be between 0 and 100'),
          backgroundColor: ValoraColors.error,
        ),
      );
      return;
    }

    if (minPrice != null && maxPrice != null && minPrice > maxPrice) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Min price cannot be greater than Max price'),
          backgroundColor: ValoraColors.error,
        ),
      );
      return;
    }

    if (minLivingArea != null &&
        maxLivingArea != null &&
        minLivingArea > maxLivingArea) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Min area cannot be greater than Max area'),
          backgroundColor: ValoraColors.error,
        ),
      );
      return;
    }

    final result = <String, dynamic>{
      'minPrice': minPrice,
      'maxPrice': maxPrice,
      'city': city,
      'minBedrooms': minBedrooms,
      'minLivingArea': minLivingArea,
      'maxLivingArea': maxLivingArea,
      'minCompositeScore': minCompositeScore,
      'minSafetyScore': minSafetyScore,
    };

    result.removeWhere((key, value) => value == null);

    Navigator.pop(context, result);
  }

  void _clear() {
    setState(() {
      _minPriceController.clear();
      _maxPriceController.clear();
      _cityController.clear();
      _minBedroomsController.clear();
      _minLivingAreaController.clear();
      _maxLivingAreaController.clear();
      _minCompositeScoreController.clear();
      _minSafetyScoreController.clear();
    });
  }

  @override
  Widget build(BuildContext context) {
    return ValoraDialog(
      title: 'Filter Listings',
      actions: [
        ValoraButton(
          label: 'Clear All',
          variant: ValoraButtonVariant.ghost,
          onPressed: _clear,
        ),
        ValoraButton(
          label: 'Apply',
          variant: ValoraButtonVariant.primary,
          onPressed: _apply,
        ),
      ],
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text('Price Range', style: ValoraTypography.titleMedium),
          const SizedBox(height: ValoraSpacing.sm),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: ValoraTextField(
                  controller: _minPriceController,
                  label: 'Min Price',
                  hint: '€ 0',
                  keyboardType: TextInputType.number,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                ),
              ),
              const SizedBox(width: ValoraSpacing.md),
              Expanded(
                child: ValoraTextField(
                  controller: _maxPriceController,
                  label: 'Max Price',
                  hint: '€ 0',
                  keyboardType: TextInputType.number,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                ),
              ),
            ],
          ),
          const SizedBox(height: ValoraSpacing.lg),
          ValoraTextField(
            controller: _cityController,
            label: 'City',
            hint: 'e.g. Amsterdam',
          ),
          const SizedBox(height: ValoraSpacing.lg),
          ValoraTextField(
            controller: _minBedroomsController,
            label: 'Min Bedrooms',
            keyboardType: TextInputType.number,
            prefixIcon: const Icon(Icons.bed_outlined),
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          ),
          const SizedBox(height: ValoraSpacing.lg),
          Text('Living Area (m²)', style: ValoraTypography.titleMedium),
          const SizedBox(height: ValoraSpacing.sm),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: ValoraTextField(
                  controller: _minLivingAreaController,
                  label: 'Min',
                  keyboardType: TextInputType.number,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                ),
              ),
              const SizedBox(width: ValoraSpacing.md),
              Expanded(
                child: ValoraTextField(
                  controller: _maxLivingAreaController,
                  label: 'Max',
                  keyboardType: TextInputType.number,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                ),
              ),
            ],
          ),
          const SizedBox(height: ValoraSpacing.lg),
          Text('Context Scores (0-100)', style: ValoraTypography.titleMedium),
          const SizedBox(height: ValoraSpacing.sm),
          Row(
            children: [
              Expanded(
                child: ValoraTextField(
                  controller: _minCompositeScoreController,
                  label: 'Min Composite',
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                ),
              ),
              const SizedBox(width: ValoraSpacing.md),
              Expanded(
                child: ValoraTextField(
                  controller: _minSafetyScoreController,
                  label: 'Min Safety',
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
