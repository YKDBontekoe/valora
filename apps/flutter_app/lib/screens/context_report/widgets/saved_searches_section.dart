import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../providers/context_report_provider.dart';
import '../../../models/saved_search.dart';

class SavedSearchesSection extends StatelessWidget {
  const SavedSearchesSection({
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

    return Selector<ContextReportProvider, List<SavedSearch>>(
      selector: (_, p) => p.savedSearches,
      builder: (context, savedSearches, _) {
        if (savedSearches.isEmpty) return const SizedBox.shrink();
        return Padding(
          padding: const EdgeInsets.fromLTRB(20, 0, 20, 0), // Adjust padding as needed
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const ValoraSectionHeader(title: 'Saved Searches'),
              const SizedBox(height: 12),
              ...savedSearches.map((item) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: ValoraCard(
                    onTap: provider.isLoading
                        ? null
                        : () {
                            controller.text = item.query;
                            provider.setRadiusMeters(item.radiusMeters);
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
                            Icons.bookmark_rounded,
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
                                '${item.radiusMeters}m radius • ${_formatDate(item.createdAt)}',
                                style: ValoraTypography.labelSmall.copyWith(
                                  color: isDark
                                      ? ValoraColors.neutral500
                                      : ValoraColors.neutral400,
                                ),
                              ),
                            ],
                          ),
                        ),
                        // Alert Toggle
                        IconButton(
                          tooltip: item.isAlertEnabled ? 'Disable Alerts' : 'Enable Alerts',
                          icon: Icon(
                            item.isAlertEnabled
                                ? Icons.notifications_active_rounded
                                : Icons.notifications_none_rounded,
                            size: 20,
                            color: item.isAlertEnabled
                                ? ValoraColors.primary
                                : ValoraColors.neutral400,
                          ),
                          onPressed: () => provider.toggleSearchAlert(item.id),
                        ),
                        // Delete
                         IconButton(
                          tooltip: 'Remove',
                          icon: Icon(
                            Icons.close_rounded,
                            size: 18,
                            color: isDark
                                ? ValoraColors.neutral500
                                : ValoraColors.neutral400,
                          ),
                          onPressed: () => _confirmRemove(context, provider, item),
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

  Future<void> _confirmRemove(
      BuildContext context, ContextReportProvider provider, SavedSearch item) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Remove Saved Search?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Remove',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child:
            Text('Are you sure you want to remove "${item.query}" from your saved searches?'),
      ),
    );

    if (confirmed == true) {
      await provider.removeSavedSearch(item.id);
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
