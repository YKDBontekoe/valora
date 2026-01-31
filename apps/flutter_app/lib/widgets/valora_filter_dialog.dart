import 'package:flutter/material.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';

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
    final city = _cityController.text.trim().isEmpty ? null : _cityController.text.trim();

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

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(ValoraSpacing.md),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Filter & Sort',
                style: Theme.of(context).textTheme.headlineSmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: ValoraSpacing.md),
              Text('Price Range', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: ValoraSpacing.xs),
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _minPriceController,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Min',
                        prefixText: '€ ',
                        border: OutlineInputBorder(),
                      ),
                    ),
                  ),
                  const SizedBox(width: ValoraSpacing.sm),
                  Expanded(
                    child: TextField(
                      controller: _maxPriceController,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Max',
                        prefixText: '€ ',
                        border: OutlineInputBorder(),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: ValoraSpacing.md),
              Text('City', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: ValoraSpacing.xs),
              TextField(
                controller: _cityController,
                decoration: const InputDecoration(
                  labelText: 'City',
                  hintText: 'e.g. Amsterdam',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: ValoraSpacing.md),
              Text('Sort By', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: ValoraSpacing.xs),
              Wrap(
                spacing: ValoraSpacing.sm,
                children: [
                  ChoiceChip(
                    label: const Text('Newest'),
                    selected: _sortBy == 'date' && _sortOrder == 'desc',
                    onSelected: (selected) {
                      if (selected) {
                        setState(() {
                          _sortBy = 'date';
                          _sortOrder = 'desc';
                        });
                      }
                    },
                  ),
                  ChoiceChip(
                    label: const Text('Price: Low to High'),
                    selected: _sortBy == 'price' && _sortOrder == 'asc',
                    onSelected: (selected) {
                      if (selected) {
                        setState(() {
                          _sortBy = 'price';
                          _sortOrder = 'asc';
                        });
                      }
                    },
                  ),
                  ChoiceChip(
                    label: const Text('Price: High to Low'),
                    selected: _sortBy == 'price' && _sortOrder == 'desc',
                    onSelected: (selected) {
                      if (selected) {
                        setState(() {
                          _sortBy = 'price';
                          _sortOrder = 'desc';
                        });
                      }
                    },
                  ),
                ],
              ),
              const SizedBox(height: ValoraSpacing.lg),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(
                    onPressed: _clear,
                    child: const Text('Clear All'),
                  ),
                  const SizedBox(width: ValoraSpacing.sm),
                  ElevatedButton(
                    onPressed: _apply,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: ValoraColors.primary,
                      foregroundColor: Colors.white,
                    ),
                    child: const Text('Apply'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
