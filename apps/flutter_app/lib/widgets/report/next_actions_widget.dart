import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';

import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/report_action.dart';
import '../common/valora_card.dart';

class NextActionsWidget extends StatelessWidget {
  const NextActionsWidget({
    super.key,
    required this.actions,
    required this.onAction,
    required this.onDismiss,
  });

  final List<ReportAction> actions;
  final ValueChanged<ReportAction> onAction;
  final ValueChanged<ReportAction> onDismiss;

  @override
  Widget build(BuildContext context) {
    if (actions.isEmpty) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: Row(
            children: [
              const Icon(Icons.bolt_rounded, size: 20, color: ValoraColors.primary),
              const SizedBox(width: 8),
              Text('Recommended Actions', style: ValoraTypography.titleMedium),
            ],
          ),
        ),
        const SizedBox(height: 12),
        SizedBox(
          height: 150,
          child: ListView.separated(
            padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
            scrollDirection: Axis.horizontal,
            itemCount: actions.length,
            separatorBuilder: (_, __) => const SizedBox(width: 12),
            itemBuilder: (context, index) {
              final action = actions[index];
              return _ActionCard(
                action: action,
                onTap: () => onAction(action),
                onDismiss: () => onDismiss(action),
              ).animate().fadeIn(delay: (index * 50).ms).slideX();
            },
          ),
        ),
      ],
    );
  }
}

class _ActionCard extends StatelessWidget {
  const _ActionCard({
    required this.action,
    required this.onTap,
    required this.onDismiss,
  });

  final ReportAction action;
  final VoidCallback onTap;
  final VoidCallback onDismiss;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    return SizedBox(
      width: 200,
      child: ValoraCard(
        onTap: onTap,
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: ValoraColors.primary.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Icon(
                    action.icon,
                    size: 20,
                    color: ValoraColors.primary,
                  ),
                ),
                const Spacer(),
                GestureDetector(
                  onTap: onDismiss,
                  behavior: HitTestBehavior.opaque,
                  child: Padding(
                    padding: const EdgeInsets.all(4),
                    child: Icon(
                      Icons.close_rounded,
                      size: 18,
                      color: colorScheme.onSurfaceVariant.withOpacity(0.6),
                    ),
                  ),
                ),
              ],
            ),
            const Spacer(),
            Text(
              action.title,
              style: ValoraTypography.labelLarge.copyWith(
                fontWeight: FontWeight.bold,
              ),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 4),
            Text(
              action.description,
              style: ValoraTypography.bodySmall.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
    );
  }
}
