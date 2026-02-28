import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../../core/theme/valora_animations.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../providers/context_report_provider.dart';

class CompareButton extends StatelessWidget {
  const CompareButton({super.key, required this.provider});
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final isComparing = provider.isComparing(
        provider.report!.location.query, provider.radiusMeters);

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () async {
          HapticFeedback.lightImpact();
          try {
            await provider.toggleComparison(
                provider.report!.location.query, provider.radiusMeters);
          } catch (e) {
            if (!context.mounted) return;
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text('Failed to add to compare: ${e.toString()}'),
                backgroundColor: ValoraColors.error,
                behavior: SnackBarBehavior.floating,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
              ),
            );
          }
        },
        borderRadius: BorderRadius.circular(12),
        child: AnimatedContainer(
          duration: ValoraAnimations.normal,
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          decoration: BoxDecoration(
            color: isComparing
                ? ValoraColors.primary.withValues(alpha: 0.15)
                : (isDark
                    ? ValoraColors.surfaceDark
                    : ValoraColors.neutral100),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: isComparing
                  ? ValoraColors.primary.withValues(alpha: 0.3)
                  : (isDark
                      ? ValoraColors.neutral700.withValues(alpha: 0.4)
                      : ValoraColors.neutral200),
            ),
          ),
          child: Icon(
            isComparing
                ? Icons.playlist_add_check_rounded
                : Icons.playlist_add_rounded,
            color:
                isComparing ? ValoraColors.primary : ValoraColors.neutral500,
            size: 22,
          ),
        ),
      ),
    );
  }
}
