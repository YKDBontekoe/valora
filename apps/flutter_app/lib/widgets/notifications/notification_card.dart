import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_spacing.dart';
import '../../models/notification.dart';
import '../../services/notification_service.dart';
import '../../widgets/valora_widgets.dart';
import '../../screens/workspace_detail_screen.dart';
import '../../screens/context_report/context_report_screen.dart';

class NotificationCard extends StatelessWidget {
  final ValoraNotification notification;
  final NotificationService provider;
  final bool isDark;
  final String Function(DateTime) formatTime;
  final IconData Function(NotificationType) getIcon;
  final Color Function(NotificationType, bool) getColor;

  const NotificationCard({
    super.key,
    required this.notification,
    required this.provider,
    required this.isDark,
    required this.formatTime,
    required this.getIcon,
    required this.getColor,
  });

  @override
  Widget build(BuildContext context) {
    final accentColor = getColor(notification.type, isDark);

    return Dismissible(
      key: Key(notification.id),
      direction: DismissDirection.endToStart,
      background: Container(
        decoration: BoxDecoration(
          color: ValoraColors.error,
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        ),
        alignment: Alignment.centerRight,
        padding: const EdgeInsets.only(right: 24),
        child: const Icon(Icons.delete_outline_rounded, color: Colors.white),
      ),
      onDismissed: (_) {
        provider.deleteNotification(notification.id);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text('Notification deleted'),
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12)),
            action: SnackBarAction(
              label: 'Undo',
              onPressed: () {
                provider.undoDelete(notification.id);
              },
            ),
          ),
        );
      },
      child: ValoraCard(
        onTap: () async {
          if (!notification.isRead) {
            provider.markAsRead(notification.id);
          }
          if (notification.actionUrl != null) {
            final actionUrl = notification.actionUrl!;
            if (actionUrl.startsWith('/workspaces/')) {
              // Parse workspace ID and optional listing ID from URL
              // Format: /workspaces/{id} or /workspaces/{id}/listings/{listingId}
              final parts = actionUrl.split('/');
              if (parts.length >= 3) {
                final workspaceId = parts[2];

                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (context) => WorkspaceDetailScreen(
                      workspaceId: workspaceId,
                    ),
                  ),
                );
                return;
              }
            } else if (actionUrl.startsWith('/reports')) {
              // Parse /reports?q={query}
              final uri = Uri.tryParse(actionUrl);
              if (uri == null) return;
              final query = uri.queryParameters['q'];

              if (query != null && query.isNotEmpty) {
                 Navigator.push(
                   context,
                   MaterialPageRoute(
                     builder: (context) => const ContextReportScreen(),
                     // Ideally we would pass the query directly to the screen to auto-fetch,
                     // but the current ContextReportScreen implementation uses a provider state
                     // and doesn't take an initial query parameter.
                     // We push the user to the report screen.
                   ),
                 );
                 return;
              }
            }

            // Fallback for non-internal URLs or unhandled paths
            final uri = Uri.tryParse(actionUrl);
            if (uri != null && await canLaunchUrl(uri)) {
              await launchUrl(uri);
            }
          }
        },
        borderColor: notification.isRead
            ? null
            : accentColor.withValues(alpha: 0.3),
        padding: const EdgeInsets.all(ValoraSpacing.md),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Icon circle
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: accentColor.withValues(alpha: 0.1),
                shape: BoxShape.circle,
              ),
              child: Icon(
                getIcon(notification.type),
                color: accentColor,
                size: 20,
              ),
            ),
            const SizedBox(width: ValoraSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          notification.title,
                          style: ValoraTypography.bodyLarge.copyWith(
                            fontWeight: notification.isRead
                                ? FontWeight.w500
                                : FontWeight.bold,
                            color: isDark
                                ? ValoraColors.neutral50
                                : ValoraColors.neutral900,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Text(
                        formatTime(notification.createdAt),
                        style: ValoraTypography.labelSmall.copyWith(
                          color: isDark
                              ? ValoraColors.neutral500
                              : ValoraColors.neutral400,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    notification.body,
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: isDark
                          ? ValoraColors.neutral300
                          : ValoraColors.neutral600,
                    ),
                  ),
                ],
              ),
            ),
            if (!notification.isRead)
              Padding(
                padding: const EdgeInsets.only(left: 8, top: 6),
                child: Container(
                  width: 8,
                  height: 8,
                  decoration: BoxDecoration(
                    color: accentColor,
                    shape: BoxShape.circle,
                    boxShadow: [
                      BoxShadow(
                        color: accentColor.withValues(alpha: 0.4),
                        blurRadius: 6,
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
