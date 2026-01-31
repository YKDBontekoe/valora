import 'package:flutter/material.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import 'valora_widgets.dart';

class ValoraFilterDialog extends StatefulWidget {
  final double? initialMinPrice;
  final double? initialMaxPrice;
  final String? initialCity;
  final String? initialSortBy;
  final String? initialSortOrder;

  const ValoraFilterDialog({
    super.key,
    this.initialMinPrice,
    this.initialMaxPrice,
    this.initialCity,
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
  String? _sortBy;
  String? _sortOrder;

  @override
  void initState() {
    super.initState();
    _minPriceController = TextEditingController(
        text: widget.initialMinPrice?.toStringAsFixed(0) ?? '');
    _maxPriceController = TextEditingController(
        text: widget.initialMaxPrice?.toStringAsFixed(0) ?? '');
    _cityController = TextEditingController(text: widget.initialCity ?? '');
    _sortBy = widget.initialSortBy ?? 'date';
    _sortOrder = widget.initialSortOrder ?? 'desc';
  }

  @override
  void dispose() {
    _minPriceController.dispose();
    _maxPriceController.dispose();
    _cityController.dispose();
    super.dispose();
  }

  void _apply() {
    final minPrice = double.tryParse(_minPriceController.text);
    final maxPrice = double.tryParse(_maxPriceController.text);
    final city =
        _cityController.text.trim().isEmpty ? null : _cityController.text.trim();

    Navigator.pop(context, {
      'minPrice': minPrice,
      'maxPrice': maxPrice,
      'city': city,
      'sortBy': _sortBy,
      'sortOrder': _sortOrder,
    });
  }

  void _clear() {
    setState(() {
      _minPriceController.clear();
      _maxPriceController.clear();
      _cityController.clear();
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
                ),
              ),
              const SizedBox(width: ValoraSpacing.md),
              Expanded(
                child: ValoraTextField(
                  controller: _maxPriceController,
                  label: 'Max Price',
                  prefixText: '€ ',
                  keyboardType: TextInputType.number,
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
            ],
          ),
        ],
      ),
    );
  }
}
