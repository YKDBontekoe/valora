import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../core/theme/valora_spacing.dart';
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
          padding: const EdgeInsets.fromLTRB(ValoraSpacing.lg, 0, ValoraSpacing.lg, 0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const ValoraSectionHeader(title: 'Saved Searches'),
              const SizedBox(height: ValoraSpacing.md),
              ...savedSearches.map((item) {
                return _SavedSearchItem(
                  item: item,
                  controller: controller,
                  provider: provider,
                  isDark: isDark,
                  formatDate: _formatDate,
                  onRemove: (context, provider, item) => _confirmRemove(context, provider, item),
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

class _SavedSearchItem extends StatefulWidget {
  final SavedSearch item;
  final TextEditingController controller;
  final ContextReportProvider provider;
  final bool isDark;
  final String Function(DateTime) formatDate;
  final Function(BuildContext, ContextReportProvider, SavedSearch) onRemove;

  const _SavedSearchItem({
    required this.item,
    required this.controller,
    required this.provider,
    required this.isDark,
    required this.formatDate,
    required this.onRemove,
  });

  @override
  State<_SavedSearchItem> createState() => _SavedSearchItemState();
}

class _SavedSearchItemState extends State<_SavedSearchItem> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: ValoraSpacing.sm),
      child: MouseRegion(
        cursor: SystemMouseCursors.click,
        onEnter: (_) => setState(() => _isHovered = true),
        onExit: (_) => setState(() => _isHovered = false),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          curve: Curves.easeOut,
          transform: _isHovered ? Matrix4.translationValues(4, 0, 0) : Matrix4.identity(),
          decoration: BoxDecoration(
            boxShadow: _isHovered ? [
                BoxShadow(
                  color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.1),
                  blurRadius: ValoraSpacing.md,
                  offset: const Offset(2, 4),
                )
              ] : null,
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          ),
          child: ValoraCard(
            onTap: widget.provider.isLoading
                ? null
                : () {
                    widget.controller.text = widget.item.query;
                    widget.provider.setRadiusMeters(widget.item.radiusMeters);
                    widget.provider.generate(widget.item.query);
                  },
            padding: const EdgeInsets.symmetric(
                horizontal: ValoraSpacing.md, vertical: ValoraSpacing.sm),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(ValoraSpacing.sm),
                  decoration: BoxDecoration(
                    color: _isHovered ? ValoraColors.primary.withValues(alpha: 0.15) : ValoraColors.primary
                        .withValues(alpha: 0.08),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.bookmark_rounded,
                    size: ValoraSpacing.md,
                    color: ValoraColors.primary,
                  ),
                ),
                const SizedBox(width: ValoraSpacing.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        widget.item.query,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: ValoraTypography.bodyMedium.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '${widget.item.radiusMeters}m radius • ${widget.formatDate(widget.item.createdAt)}',
                        style: ValoraTypography.labelSmall.copyWith(
                          color: widget.isDark
                              ? ValoraColors.neutral500
                              : ValoraColors.neutral400,
                        ),
                      ),
                    ],
                  ),
                ),
                // Alert Toggle
                IconButton(
                  tooltip: widget.item.isAlertEnabled ? 'Disable Alerts' : 'Enable Alerts',
                  icon: Icon(
                    widget.item.isAlertEnabled
                        ? Icons.notifications_active_rounded
                        : Icons.notifications_none_rounded,
                    size: ValoraSpacing.iconSizeSm,
                    color: widget.item.isAlertEnabled
                        ? ValoraColors.primary
                        : ValoraColors.neutral400,
                  ),
                  onPressed: () => widget.provider.toggleSearchAlert(widget.item.id),
                ),
                // Delete
                 IconButton(
                  tooltip: 'Remove',
                  icon: Icon(
                    Icons.close_rounded,
                    size: 18,
                    color: widget.isDark
                        ? ValoraColors.neutral500
                        : ValoraColors.neutral400,
                  ),
                  onPressed: () => widget.onRemove(context, widget.provider, widget.item),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
