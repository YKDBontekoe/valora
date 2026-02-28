import 'package:flutter/material.dart';
import '../../models/activity_log.dart';
import 'package:intl/intl.dart';
import '../valora_widgets.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';

class ActivityFeedWidget extends StatelessWidget {
  final List<ActivityLog> activities;

  const ActivityFeedWidget({super.key, required this.activities});

  @override
  Widget build(BuildContext context) {
    if (activities.isEmpty) {
      return Center(
        child: ValoraEmptyState(
          icon: Icons.history_rounded,
          title: 'No recent activity',
          subtitle: 'Recent actions in this workspace will appear here.',
        ),
      );
    }

    final isDark = Theme.of(context).brightness == Brightness.dark;

    return ListView.separated(
      padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md, vertical: ValoraSpacing.sm),
      itemCount: activities.length,
      separatorBuilder: (context, index) => const SizedBox(height: ValoraSpacing.sm),
      itemBuilder: (context, index) {
        final activity = activities[index];
        return ValoraCard(
          padding: const EdgeInsets.all(ValoraSpacing.md),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: isDark ? ValoraColors.neutral800 : ValoraColors.neutral100,
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  Icons.history_rounded,
                  size: 20,
                  color: isDark ? ValoraColors.neutral300 : ValoraColors.neutral600,
                ),
              ),
              const SizedBox(width: ValoraSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      activity.summary,
                      style: ValoraTypography.bodyMedium.copyWith(
                        fontWeight: FontWeight.w500,
                        color: isDark ? ValoraColors.neutral200 : ValoraColors.neutral800,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${activity.actorId} • ${DateFormat.yMMMd().add_jm().format(activity.createdAt)}',
                      style: ValoraTypography.labelSmall.copyWith(
                        color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
