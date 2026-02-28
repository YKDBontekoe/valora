import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../providers/context_report_provider.dart';

class HistorySection extends StatelessWidget {
  const HistorySection({
    super.key,
    required this.controller,
    required this.provider,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Selector<ContextReportProvider, List<dynamic>>(
      selector: (_, p) => p.history,
      builder: (context, history, _) {
        if (history.isEmpty) return const SizedBox.shrink();
        return Padding(
          padding: const EdgeInsets.fromLTRB(20, 32, 20, 0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const ValoraSectionHeader(title: 'Recent'),
                  TextButton(
                    onPressed: () => _confirmClearHistory(context, provider),
                    child: Text(
                      'Clear',
                      style: ValoraTypography.labelMedium
                          .copyWith(color: theme.colorScheme.primary),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              ...history.take(5).map((item) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: ValoraCard(
                    onTap: provider.isLoading
                        ? null
                        : () {
                            controller.text = item.query;
                            provider.generate(item.query);
                          },
                    padding: const EdgeInsets.symmetric(
                        horizontal: 16, vertical: 14),
                    child: Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: ValoraColors.primary
                                .withValues(alpha: 0.08),
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(
                            Icons.history_rounded,
                            size: 16,
                            color: ValoraColors.primary,
                          ),
                        ),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                item.query,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: ValoraTypography.bodyMedium.copyWith(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                _formatDate(item.timestamp),
                                style: ValoraTypography.labelSmall.copyWith(
                                  color: isDark
                                      ? ValoraColors.neutral500
                                      : ValoraColors.neutral400,
                                ),
                              ),
                            ],
                          ),
                        ),
                        // Compare toggle
                        IconButton(
                          tooltip: provider.isComparing(
                                  item.query, provider.radiusMeters)
                              ? 'Remove from Compare'
                              : 'Add to Compare',
                          icon: Icon(
                            provider.isComparing(
                                    item.query, provider.radiusMeters)
                                ? Icons.playlist_add_check_rounded
                                : Icons.playlist_add_rounded,
                            size: 20,
                            color: provider.isComparing(
                                    item.query, provider.radiusMeters)
                                ? ValoraColors.primary
                                : ValoraColors.neutral400,
                          ),
                          onPressed: () async {
                            try {
                              await provider.toggleComparison(
                                  item.query, provider.radiusMeters);
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
                        ),
                        Icon(
                          Icons.chevron_right_rounded,
                          size: 20,
                          color: isDark
                              ? ValoraColors.neutral500
                              : ValoraColors.neutral400,
                        ),
                      ],
                    ),
                  ),
                );
              }),
            ],
          ),
        );
      },
    );
  }

  Future<void> _confirmClearHistory(
      BuildContext context, ContextReportProvider provider) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Clear History?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Clear',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child:
            const Text('Are you sure you want to clear your search history?'),
      ),
    );

    if (confirmed == true) {
      await provider.clearHistory();
    }
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final yesterday = today.subtract(const Duration(days: 1));
    final midnightDate = DateTime(date.year, date.month, date.day);

    if (midnightDate.isAfter(today)) {
      return DateFormat('dd/MM/yyyy').format(date);
    }

    if (midnightDate == today) return 'Today';
    if (midnightDate == yesterday) return 'Yesterday';

    return DateFormat('dd/MM/yyyy').format(date);
  }
}
