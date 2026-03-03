import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../core/theme/valora_spacing.dart';
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
          padding: const EdgeInsets.fromLTRB(ValoraSpacing.lg, ValoraSpacing.xl, ValoraSpacing.lg, 0),
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
              const SizedBox(height: ValoraSpacing.md),
              ...history.take(5).map((item) {
                return _HistoryItem(
                  item: item,
                  controller: controller,
                  provider: provider,
                  isDark: isDark,
                  formatDate: _formatDate,
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

class _HistoryItem extends StatefulWidget {
  final dynamic item;
  final TextEditingController controller;
  final ContextReportProvider provider;
  final bool isDark;
  final String Function(DateTime) formatDate;

  const _HistoryItem({
    required this.item,
    required this.controller,
    required this.provider,
    required this.isDark,
    required this.formatDate,
  });

  @override
  State<_HistoryItem> createState() => _HistoryItemState();
}

class _HistoryItemState extends State<_HistoryItem> {
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
                    Icons.history_rounded,
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
                        widget.formatDate(widget.item.timestamp),
                        style: ValoraTypography.labelSmall.copyWith(
                          color: widget.isDark
                              ? ValoraColors.neutral500
                              : ValoraColors.neutral400,
                        ),
                      ),
                    ],
                  ),
                ),
                // Compare toggle
                IconButton(
                  tooltip: widget.provider.isComparing(
                          widget.item.query, widget.provider.radiusMeters)
                      ? 'Remove from Compare'
                      : 'Add to Compare',
                  icon: Icon(
                    widget.provider.isComparing(
                            widget.item.query, widget.provider.radiusMeters)
                        ? Icons.playlist_add_check_rounded
                        : Icons.playlist_add_rounded,
                    size: ValoraSpacing.iconSizeSm,
                    color: widget.provider.isComparing(
                            widget.item.query, widget.provider.radiusMeters)
                        ? ValoraColors.primary
                        : ValoraColors.neutral400,
                  ),
                  onPressed: () async {
                    try {
                      await widget.provider.toggleComparison(
                          widget.item.query, widget.provider.radiusMeters);
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
                  size: ValoraSpacing.iconSizeSm,
                  color: _isHovered ? ValoraColors.primary : widget.isDark
                      ? ValoraColors.neutral500
                      : ValoraColors.neutral400,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
