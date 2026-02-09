import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import 'valora_widgets.dart';

class ValoraFilterDialog extends StatefulWidget {
  final double? initialMinPrice;
  final double? initialMaxPrice;
  final String? initialCity;
  final int? initialMinBedrooms;
  final int? initialMinLivingArea;
  final int? initialMaxLivingArea;
  final double? initialMinSafetyScore;
  final double? initialMinCompositeScore;
  final String? initialSortBy;
  final String? initialSortOrder;

  const ValoraFilterDialog({
    super.key,
    this.initialMinPrice,
    this.initialMaxPrice,
    this.initialCity,
    this.initialMinBedrooms,
    this.initialMinLivingArea,
    this.initialMaxLivingArea,
    this.initialMinSafetyScore,
    this.initialMinCompositeScore,
    this.initialSortBy,
    this.initialSortOrder,
  });

  @override
  State<ValoraFilterDialog> createState() => _ValoraFilterDialogState();
}

class _ValoraFilterDialogState extends State<ValoraFilterDialog> {
  late TextEditingController _minPriceController;
  late TextEditingController _maxPriceController;
  late TextEditingController _cityController;
  late TextEditingController _minBedroomsController;
  late TextEditingController _minLivingAreaController;
  late TextEditingController _maxLivingAreaController;
  late TextEditingController _minCompositeScoreController;
  late TextEditingController _minSafetyScoreController;
  String? _sortBy;
  String? _sortOrder;

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
    _sortBy = widget.initialSortBy ?? 'date';
    _sortOrder = widget.initialSortOrder ?? 'desc';
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
    final minCompositeScore = double.tryParse(_minCompositeScoreController.text);
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

    Navigator.pop(context, {
      'minPrice': minPrice,
      'maxPrice': maxPrice,
      'city': city,
      'minBedrooms': minBedrooms,
      'minLivingArea': minLivingArea,
      'maxLivingArea': maxLivingArea,
      'minCompositeScore': minCompositeScore,
      'minSafetyScore': minSafetyScore,
      'sortBy': _sortBy,
      'sortOrder': _sortOrder,
    });
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
      _sortBy = 'date';
      _sortOrder = 'desc';
    });
  }

  void _updateSort(String by, String order) {
    setState(() {
      _sortBy = by;
      _sortOrder = order;
    });
  }

  @override
  Widget build(BuildContext context) {
    return ValoraDialog(
      title: 'Filter & Sort',
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
            prefixIcon: Icons.bed_outlined,
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
          Text('Sort By', style: ValoraTypography.titleMedium),
          const SizedBox(height: ValoraSpacing.sm),
          Wrap(
            spacing: ValoraSpacing.sm,
            runSpacing: ValoraSpacing.xs,
            children: [
              ValoraChip(
                label: 'Newest',
                isSelected: _sortBy == 'date' && _sortOrder == 'desc',
                onSelected: (selected) {
                  if (selected) _updateSort('date', 'desc');
                },
              ),
              ValoraChip(
                label: 'Price: Low to High',
                isSelected: _sortBy == 'price' && _sortOrder == 'asc',
                onSelected: (selected) {
                  if (selected) _updateSort('price', 'asc');
                },
              ),
              ValoraChip(
                label: 'Price: High to Low',
                isSelected: _sortBy == 'price' && _sortOrder == 'desc',
                onSelected: (selected) {
                  if (selected) _updateSort('price', 'desc');
                },
              ),
              ValoraChip(
                label: 'Area: Small to Large',
                isSelected: _sortBy == 'livingarea' && _sortOrder == 'asc',
                onSelected: (selected) {
                  if (selected) _updateSort('livingarea', 'asc');
                },
              ),
              ValoraChip(
                label: 'Area: Large to Small',
                isSelected: _sortBy == 'livingarea' && _sortOrder == 'desc',
                onSelected: (selected) {
                  if (selected) _updateSort('livingarea', 'desc');
                },
              ),
              ValoraChip(
                label: 'Composite Score: High to Low',
                isSelected: _sortBy == 'contextcompositescore' && _sortOrder == 'desc',
                onSelected: (selected) {
                  if (selected) _updateSort('contextcompositescore', 'desc');
                },
              ),
              ValoraChip(
                label: 'Safety Score: High to Low',
                isSelected: _sortBy == 'contextsafetyscore' && _sortOrder == 'desc',
                onSelected: (selected) {
                  if (selected) _updateSort('contextsafetyscore', 'desc');
                },
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
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                ),
              ),
              const SizedBox(width: ValoraSpacing.md),
              Expanded(
                child: ValoraTextField(
                  controller: _minSafetyScoreController,
                  label: 'Min Safety',
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
