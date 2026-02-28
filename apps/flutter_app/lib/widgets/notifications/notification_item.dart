import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/notification.dart';

class NotificationItem extends StatelessWidget {
  final ValoraNotification notification;
  final VoidCallback? onTap;

  const NotificationItem({
    super.key,
    required this.notification,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return InkWell(
      onTap: onTap,
      child: Container(
        color: notification.isRead
            ? null
            : (isDark
                ? ValoraColors.primary.withValues(alpha: 0.1)
                : ValoraColors.primary.withValues(alpha: 0.05)),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildIcon(context),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    notification.title,
                    style: ValoraTypography.bodyLarge.copyWith(
                      fontWeight: notification.isRead
                          ? FontWeight.w500
                          : FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    notification.body,
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      _buildCategoryBadge(context),
                      const SizedBox(width: 8),
                      Text(
                        _formatTime(notification.createdAt),
                        style: ValoraTypography.labelSmall.copyWith(
                          color: Theme.of(context).colorScheme.outline,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            if (!notification.isRead)
              Container(
                width: 8,
                height: 8,
                decoration: const BoxDecoration(
                  color: ValoraColors.primary,
                  shape: BoxShape.circle,
                ),
              ),
          ],
        ),
      ),
    );
  }


  Widget _buildCategoryBadge(BuildContext context) {
    final String label;
    final Color color;

    switch (notification.type) {
      case NotificationType.priceDrop:
        label = 'Price Drop';
        color = ValoraColors.success;
        break;
      case NotificationType.newListing:
        label = 'New Listing';
        color = ValoraColors.primary;
        break;
      case NotificationType.system:
        label = 'System';
        color = ValoraColors.neutral500;
        break;
      case NotificationType.info:
        label = 'Info';
        color = ValoraColors.info;
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(4),
        border: Border.all(color: color.withValues(alpha: 0.5)),
      ),
      child: Text(
        label,
        style: ValoraTypography.labelSmall.copyWith(
          color: color,
          fontSize: 10,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }

  Widget _buildIcon(BuildContext context) {
    final IconData iconData;
    final Color color;

    switch (notification.type) {
      case NotificationType.priceDrop:
        iconData = Icons.trending_down_rounded;
        color = ValoraColors.success;
        break;
      case NotificationType.newListing:
        iconData = Icons.home_work_rounded;
        color = ValoraColors.primary;
        break;
      case NotificationType.system:
        iconData = Icons.settings_rounded;
        color = ValoraColors.neutral500;
        break;
      case NotificationType.info:

        iconData = Icons.info_outline_rounded;
        color = ValoraColors.info;
        break;
    }

    return Container(
      width: 40,
      height: 40,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        shape: BoxShape.circle,
      ),
      child: Icon(iconData, color: color, size: 20),
    );
  }

  String _formatTime(DateTime dateTime) {
    final now = DateTime.now();
    final diff = now.difference(dateTime);

    if (diff.inMinutes < 60) {
      return '${diff.inMinutes}m ago';
    } else if (diff.inHours < 24) {
      return '${diff.inHours}h ago';
    } else if (diff.inDays < 7) {
      return '${diff.inDays}d ago';
    } else {
      return DateFormat('MMM d').format(dateTime);
    }
  }
}
