import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/activity_log.dart';
import '../valora_widgets.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';

class ActivityFeedWidget extends StatelessWidget {
  final List<ActivityLog> activities;

  const ActivityFeedWidget({super.key, required this.activities});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    if (activities.isEmpty) {
      return Center(
        child: ValoraEmptyState(
          icon: Icons.history_toggle_off_rounded,
          title: 'No recent activity',
          subtitle: 'Actions performed in this workspace will appear here.',
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(ValoraSpacing.md),
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
                padding: const EdgeInsets.all(ValoraSpacing.sm),
                decoration: BoxDecoration(
                  color: (isDark ? ValoraColors.primaryLight : ValoraColors.primary).withValues(alpha: 0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  Icons.history_rounded,
                  size: 20,
                  color: isDark ? ValoraColors.primaryLight : ValoraColors.primary,
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
                        fontWeight: FontWeight.w600,
                        color: isDark ? ValoraColors.neutral100 : ValoraColors.neutral900,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        Icon(
                          Icons.person_outline_rounded,
                          size: 14,
                          color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          activity.actorId,
                          style: ValoraTypography.labelSmall.copyWith(
                            color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                          ),
                        ),
                        const SizedBox(width: ValoraSpacing.sm),
                        Text(
                          '•',
                          style: ValoraTypography.labelSmall.copyWith(
                            color: isDark ? ValoraColors.neutral600 : ValoraColors.neutral300,
                          ),
                        ),
                        const SizedBox(width: ValoraSpacing.sm),
                        Text(
                          DateFormat.yMMMd().add_jm().format(activity.createdAt),
                          style: ValoraTypography.labelSmall.copyWith(
                            color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                          ),
                        ),
                      ],
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
