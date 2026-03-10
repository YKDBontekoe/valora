import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../core/theme/valora_spacing.dart';
import 'package:flutter_animate/flutter_animate.dart';
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

    return Selector<ContextReportProvider, List<SavedSearch>>(
      selector: (_, p) => p.savedSearches,
      builder: (context, savedSearches, _) {
        if (savedSearches.isEmpty) return const SizedBox.shrink();
        return Padding(
          padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const ValoraSectionHeader(title: 'Saved Searches'),
              const SizedBox(height: ValoraSpacing.sm),
              ...savedSearches.asMap().entries.map((entry) {
                final index = entry.key;
                final item = entry.value;
                return Padding(
                  padding: const EdgeInsets.only(bottom: ValoraSpacing.sm),
                  child: ValoraCard(
                    elevation: ValoraSpacing.elevationSm,
                    onTap: provider.isLoading
                        ? null
                        : () {
                            controller.text = item.query;
                            provider.setRadiusMeters(item.radiusMeters);
                            provider.generate(item.query);
                          },
                    padding: const EdgeInsets.all(ValoraSpacing.md),
                    child: Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(ValoraSpacing.sm),
                          decoration: BoxDecoration(
                            color: theme.colorScheme.primary
                                .withValues(alpha: 0.08),
                            shape: BoxShape.circle,
                          ),
                          child: Icon(
                            Icons.bookmark_rounded,
                            size: ValoraSpacing.iconSizeSm,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                        const SizedBox(width: ValoraSpacing.md),
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
                              const SizedBox(height: ValoraSpacing.xs),
                              Text(
                                '${item.radiusMeters}m radius • ${_formatDate(item.createdAt)}',
                                style: ValoraTypography.labelSmall.copyWith(
                                  color: theme.colorScheme.onSurfaceVariant,
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
                            size: ValoraSpacing.iconSizeMd,
                            color: item.isAlertEnabled
                                ? theme.colorScheme.primary
                                : theme.colorScheme.onSurfaceVariant,
                          ),
                          onPressed: () => provider.toggleSearchAlert(item.id),
                        ),
                        // Delete
                         IconButton(
                          tooltip: 'Remove',
                          icon: Icon(
                            Icons.close_rounded,
                            size: ValoraSpacing.iconSizeMd,
                            color: theme.colorScheme.onSurfaceVariant,
                          ),
                          onPressed: () => _confirmRemove(context, provider, item),
                        ),
                      ],
                    ),
                  ),
                ).animate().fadeIn(duration: 400.ms, delay: (100 * index).ms).slideX(begin: -0.05);
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
