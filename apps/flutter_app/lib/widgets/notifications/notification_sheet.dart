import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../../services/notification_service.dart';
import '../valora_widgets.dart';
import 'notification_item.dart';

class NotificationSheet extends StatefulWidget {
  const NotificationSheet({super.key});

  @override
  State<NotificationSheet> createState() => _NotificationSheetState();
}

class _NotificationSheetState extends State<NotificationSheet> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationService>().fetchNotifications();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
      ),
      child: Consumer<NotificationService>(
        builder: (context, service, _) {
          return Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(24, 16, 16, 8),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Notifications',
                      style: ValoraTypography.headlineSmall.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    if (service.notifications.isNotEmpty)
                      TextButton(
                        onPressed: service.isLoading
                            ? null
                            : () => service.markAllAsRead(),
                        child: const Text('Mark all as read'),
                      ),
                  ],
                ),
              ),
              const Divider(height: 1),
              if (service.isLoading && service.notifications.isEmpty)
                const SizedBox(
                  height: 200,
                  child: Center(child: CircularProgressIndicator()),
                )
              else if (service.error != null && service.notifications.isEmpty)
                SizedBox(
                  height: 200,
                  child: Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.error_outline_rounded,
                            size: 48, color: ValoraColors.error),
                        const SizedBox(height: 16),
                        Text(
                          'Something went wrong',
                          style: ValoraTypography.bodyMedium,
                        ),
                        TextButton(
                          onPressed: () => service.fetchNotifications(refresh: true),
                          child: const Text('Try Again'),
                        ),
                      ],
                    ),
                  ),
                )
              else if (service.notifications.isEmpty)
                SizedBox(
                  height: 200,
                  child: Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.notifications_none_rounded,
                          size: 48,
                          color: Theme.of(context).colorScheme.outline,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'No notifications yet',
                          style: ValoraTypography.bodyMedium.copyWith(
                            color: Theme.of(context).colorScheme.onSurfaceVariant,
                          ),
                        ),
                      ],
                    ),
                  ),
                )
              else
                Flexible(
                  child: ListView.separated(
                    shrinkWrap: true,
                    itemCount: service.notifications.length,
                    separatorBuilder: (context, index) => const Divider(
                      height: 1,
                      indent: 72,
                    ),
                    itemBuilder: (context, index) {
                      final notification = service.notifications[index];
                      return Dismissible(
                        key: Key(notification.id),
                        direction: DismissDirection.endToStart,
                        background: Container(
                          color: ValoraColors.error,
                          alignment: Alignment.centerRight,
                          padding: const EdgeInsets.only(right: 24),
                          child: const Icon(
                            Icons.delete_outline_rounded,
                            color: Colors.white,
                          ),
                        ),
                        confirmDismiss: (direction) async {
                          return await showDialog<bool>(
                            context: context,
                            builder: (context) => ValoraDialog(
                              title: 'Delete Notification?',
                              actions: [
                                ValoraButton(
                                  label: 'Cancel',
                                  variant: ValoraButtonVariant.ghost,
                                  onPressed: () => Navigator.pop(context, false),
                                ),
                                ValoraButton(
                                  label: 'Delete',
                                  variant: ValoraButtonVariant.primary,
                                  onPressed: () => Navigator.pop(context, true),
                                ),
                              ],
                              child: const Text(
                                'Are you sure you want to delete this notification?',
                              ),
                            ),
                          );
                        },
                        onDismissed: (direction) {
                          service.deleteNotification(notification.id);
                        },
                        child: NotificationItem(
                          notification: notification,
                          onTap: () async {
                            if (!notification.isRead) {
                              service.markAsRead(notification.id);
                            }
                            if (notification.actionUrl != null) {
                              final uri = Uri.parse(notification.actionUrl!);
                              if (await canLaunchUrl(uri)) {
                                await launchUrl(uri);
                              }
                            }
                          },
                        ),
                      );
                    },
                  ),
                ),
            ],
          );
        },
      ),
    );
  }
}
