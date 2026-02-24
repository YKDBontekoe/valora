import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../../core/theme/valora_animations.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../providers/context_report_provider.dart';

class SaveSearchButton extends StatelessWidget {
  const SaveSearchButton({super.key, required this.provider});
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    if (provider.report == null) return const SizedBox.shrink();

    final isDark = Theme.of(context).brightness == Brightness.dark;
    final query = provider.report!.location.query;
    final radius = provider.radiusMeters;
    final isSaved = provider.isSearchSaved(query, radius);

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () {
          HapticFeedback.lightImpact();
          if (isSaved) {
            // Find ID and remove
            final search = provider.savedSearches.firstWhere(
                (s) => s.query.toLowerCase() == query.toLowerCase() && s.radiusMeters == radius);
            provider.removeSavedSearch(search.id);
          } else {
            provider.saveSearch(query, radius);
            // Maybe show a snackbar?
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: const Text('Search saved'),
                duration: const Duration(seconds: 2),
                behavior: SnackBarBehavior.floating,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
            );
          }
        },
        borderRadius: BorderRadius.circular(12),
        child: AnimatedContainer(
          duration: ValoraAnimations.normal,
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          decoration: BoxDecoration(
            color: isSaved
                ? ValoraColors.primary.withValues(alpha: 0.15)
                : (isDark
                    ? ValoraColors.surfaceDark
                    : ValoraColors.neutral100),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: isSaved
                  ? ValoraColors.primary.withValues(alpha: 0.3)
                  : (isDark
                      ? ValoraColors.neutral700.withValues(alpha: 0.4)
                      : ValoraColors.neutral200),
            ),
          ),
          child: Icon(
            isSaved
                ? Icons.bookmark_rounded
                : Icons.bookmark_border_rounded,
            color:
                isSaved ? ValoraColors.primary : ValoraColors.neutral500,
            size: 22,
          ),
        ),
      ),
    );
  }
}
