import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/activity_log.dart';
import 'package:intl/intl.dart';
import '../valora_widgets.dart';

class ActivityFeedWidget extends StatelessWidget {
  final List<ActivityLog> activities;

  const ActivityFeedWidget({super.key, required this.activities});

  IconData _getIcon(ActivityLogType type) {
    switch (type) {
      case ActivityLogType.workspaceCreated:
        return Icons.add_business_rounded;
      case ActivityLogType.memberInvited:
        return Icons.person_add_rounded;
      case ActivityLogType.memberJoined:
        return Icons.how_to_reg_rounded;
      case ActivityLogType.memberRemoved:
        return Icons.person_remove_rounded;
      case ActivityLogType.listingSaved:
        return Icons.bookmark_add_rounded;
      case ActivityLogType.listingRemoved:
        return Icons.bookmark_remove_rounded;
      case ActivityLogType.commentAdded:
        return Icons.chat_bubble_outline_rounded;
      case ActivityLogType.commentReplied:
        return Icons.reply_rounded;
      case ActivityLogType.roleChanged:
        return Icons.manage_accounts_rounded;
      case ActivityLogType.workspaceDeleted:
        return Icons.delete_outline_rounded;
      case ActivityLogType.workspaceUpdated:
        return Icons.edit_rounded;
      case ActivityLogType.unknown:
        return Icons.info_outline_rounded;
    }
  }

  Color _getColor(ActivityLogType type) {
    switch (type) {
      case ActivityLogType.workspaceCreated:
      case ActivityLogType.memberJoined:
      case ActivityLogType.listingSaved:
        return ValoraColors.success;
      case ActivityLogType.memberRemoved:
      case ActivityLogType.listingRemoved:
      case ActivityLogType.workspaceDeleted:
        return ValoraColors.error;
      case ActivityLogType.memberInvited:
      case ActivityLogType.commentAdded:
      case ActivityLogType.commentReplied:
      case ActivityLogType.roleChanged:
      case ActivityLogType.workspaceUpdated:
        return ValoraColors.primary;
      case ActivityLogType.unknown:
        return ValoraColors.neutral400;
    }
  }

  @override
  Widget build(BuildContext context) {
    if (activities.isEmpty) {
      return Center(
        child: ValoraEmptyState(
          icon: Icons.history_rounded,
          title: 'No recent activity',
          subtitle: 'Changes made to this workspace will appear here.',
        ),
      );
    }

    // Group activities by date
    final grouped = <String, List<ActivityLog>>{};
    for (var activity in activities) {
      final date = DateFormat.yMMMd().format(activity.createdAt);
      grouped.putIfAbsent(date, () => []).add(activity);
    }

    return ListView.builder(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      itemCount: grouped.length,
      itemBuilder: (context, index) {
        final date = grouped.keys.elementAt(index);
        final dateActivities = grouped[date]!;

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.only(bottom: ValoraSpacing.sm, top: ValoraSpacing.sm),
              child: Text(
                date,
                style: ValoraTypography.labelLarge.copyWith(
                  fontWeight: FontWeight.bold,
                  color: ValoraColors.neutral500,
                ),
              ),
            ),
            ...dateActivities.map((activity) {
              return Padding(
                padding: const EdgeInsets.only(bottom: ValoraSpacing.sm),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: _getColor(activity.type).withValues(alpha: 0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        _getIcon(activity.type),
                        size: 16,
                        color: _getColor(activity.type),
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
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            DateFormat.jm().format(activity.createdAt),
                            style: ValoraTypography.labelSmall.copyWith(
                              color: ValoraColors.neutral500,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              );
            }),
          ],
        );
      },
    );
  }
}
