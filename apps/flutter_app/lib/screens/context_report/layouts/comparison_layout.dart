import 'package:flutter/material.dart';
import '../../../widgets/report/comparison_view.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_typography.dart';

class ComparisonLayout extends StatelessWidget {
  const ComparisonLayout({
    super.key,
    required this.onBack,
    required this.onClear,
  });

  final VoidCallback onBack;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        backgroundColor: colorScheme.surface.withValues(alpha: 0.95),
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: onBack,
        ),
        title: Text(
          'Compare Properties',
          style: ValoraTypography.titleLarge.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          IconButton(
            tooltip: 'Clear all',
            icon: const Icon(Icons.delete_sweep_rounded),
            onPressed: () async {
              final confirmed = await showDialog<bool>(
                context: context,
                builder: (context) => ValoraDialog(
                  title: 'Clear Comparison?',
                  actions: [
                    ValoraButton(
                      label: 'Cancel',
                      variant: ValoraButtonVariant.ghost,
                      onPressed: () => Navigator.pop(context, false),
                    ),
                    ValoraButton(
                      label: 'Clear All',
                      variant: ValoraButtonVariant.primary,
                      onPressed: () => Navigator.pop(context, true),
                    ),
                  ],
                  child: const Text('Are you sure you want to clear all reports from comparison?'),
                ),
              );

              if (confirmed == true && context.mounted) {
                onClear();
                onBack();
              }
            },
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: const ComparisonView(),
    );
  }
}
