import 'package:flutter/material.dart';
import '../../../widgets/report/comparison_view.dart';
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
            onPressed: onClear,
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: const ComparisonView(),
    );
  }
}
