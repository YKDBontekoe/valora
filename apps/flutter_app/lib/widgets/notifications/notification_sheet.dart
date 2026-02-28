import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../../services/notification_service.dart';
import '../valora_widgets.dart';
import 'notification_item.dart';
import '../../screens/workspace_detail_screen.dart';
import '../../screens/context_report/context_report_screen.dart';

class NotificationSheet extends StatefulWidget {
  const NotificationSheet({super.key});

  @override
  State<NotificationSheet> createState() => _NotificationSheetState();
}

class _NotificationSheetState extends State<NotificationSheet> {
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationService>().fetchNotifications();
    });
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 200) {
      context.read<NotificationService>().loadMoreNotifications();
    }
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
                    controller: _scrollController,
                    shrinkWrap: true,
                    itemCount: service.notifications.length +
                        (service.isLoadingMore ? 1 : 0),
                    separatorBuilder: (context, index) => const Divider(
                      height: 1,
                      indent: 72,
                    ),
                    itemBuilder: (context, index) {
                      if (index == service.notifications.length) {
                        return const Padding(
                          padding: EdgeInsets.all(16.0),
                          child: Center(child: CircularProgressIndicator()),
                        );
                      }
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
                        onDismissed: (direction) async {
                          try {
                            await service.deleteNotification(notification.id);
                          } catch (_) {
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text('Failed to delete notification'),
                                  backgroundColor: ValoraColors.error,
                                ),
                              );
                            }
                          }
                        },
                        child: NotificationItem(
                          notification: notification,
                          onTap: () async {
                            if (!notification.isRead) {
                              service.markAsRead(notification.id);
                            }
                            if (notification.actionUrl != null) {
                              final actionUrl = notification.actionUrl!;
                              if (actionUrl.startsWith('/workspaces/')) {
                                final parts = actionUrl.split('/');
                                if (parts.length >= 3) {
                                  final workspaceId = parts[2];
                                  Navigator.pop(context); // Close sheet
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
                                final uri = Uri.parse(actionUrl);
                                final query = uri.queryParameters['q'];

                                if (query != null && query.isNotEmpty) {
                                  Navigator.pop(context); // Close sheet
                                  Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                      builder: (context) => const ContextReportScreen(),
                                    ),
                                  );
                                  return;
                                }
                              }

                              final uri = Uri.parse(actionUrl);
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
