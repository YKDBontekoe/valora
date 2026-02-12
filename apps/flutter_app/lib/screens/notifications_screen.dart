import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../models/notification.dart';
import '../services/notification_service.dart';
import '../widgets/valora_widgets.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);

    // Initial fetch if needed
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<NotificationService>();
      if (provider.notifications.isEmpty) {
        provider.fetchNotifications();
      }
    });
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    final provider = context.read<NotificationService>();
    if (_scrollController.position.pixels >=
            _scrollController.position.maxScrollExtent - 200 &&
        !provider.isLoadingMore &&
        provider.hasMore &&
        provider.error == null) {
      provider.loadMoreNotifications();
    }
  }

  Future<void> _handleRefresh() async {
    await context.read<NotificationService>().fetchNotifications(refresh: true);
  }

  String _formatTime(DateTime dateTime) {
    final now = DateTime.now();
    final difference = now.difference(dateTime);

    if (difference.inDays > 7) {
      return '${dateTime.day}/${dateTime.month}/${dateTime.year}';
    } else if (difference.inDays >= 1) {
      return '${difference.inDays}d ago';
    } else if (difference.inHours >= 1) {
      return '${difference.inHours}h ago';
    } else if (difference.inMinutes >= 1) {
      return '${difference.inMinutes}m ago';
    } else {
      return 'Just now';
    }
  }

  IconData _getIconForType(NotificationType type) {
    switch (type) {
      case NotificationType.priceDrop:
        return Icons.trending_down_rounded;
      case NotificationType.newListing:
        return Icons.home_rounded;
      case NotificationType.system:
        return Icons.info_outline_rounded;
      case NotificationType.info:
        return Icons.notifications_none_rounded;
    }
  }

  Color _getColorForType(NotificationType type, bool isDark) {
    switch (type) {
      case NotificationType.priceDrop:
        return ValoraColors.success;
      case NotificationType.newListing:
        return ValoraColors.primary;
      case NotificationType.system:
        return isDark ? ValoraColors.neutral400 : ValoraColors.neutral600;
      case NotificationType.info:
        return isDark ? ValoraColors.neutral400 : ValoraColors.neutral600;
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Consumer<NotificationService>(
      builder: (context, provider, child) {
        return Scaffold(
          body: RefreshIndicator(
            onRefresh: _handleRefresh,
            child: CustomScrollView(
              controller: _scrollController,
              physics: const AlwaysScrollableScrollPhysics(),
              slivers: [
                SliverAppBar(
                  pinned: true,
                  backgroundColor: isDark
                      ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
                      : ValoraColors.backgroundLight.withValues(alpha: 0.95),
                  surfaceTintColor: Colors.transparent,
                  title: Text(
                    'Notifications',
                    style: ValoraTypography.headlineMedium.copyWith(
                      color: isDark
                          ? ValoraColors.neutral50
                          : ValoraColors.neutral900,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  centerTitle: false,
                  actions: [
                    if (provider.unreadCount > 0)
                      TextButton.icon(
                        onPressed: () => provider.markAllAsRead(),
                        icon: const Icon(Icons.done_all_rounded, size: 18),
                        label: const Text('Mark all read'),
                        style: TextButton.styleFrom(
                          foregroundColor: ValoraColors.primary,
                        ),
                      ),
                    const SizedBox(width: 8),
                  ],
                ),
                if (provider.isLoading && provider.notifications.isEmpty)
                   const SliverFillRemaining(
                    child: Center(
                      child: ValoraLoadingIndicator(message: 'Loading notifications...'),
                    ),
                  )
                else if (provider.error != null && provider.notifications.isEmpty)
                  SliverFillRemaining(
                    hasScrollBody: false,
                    child: Center(
                      child: ValoraEmptyState(
                        icon: Icons.error_outline_rounded,
                        title: 'Something went wrong',
                        subtitle: provider.error ?? 'Unknown error',
                        action: ValoraButton(
                          label: 'Retry',
                          onPressed: _handleRefresh,
                        ),
                      ),
                    ),
                  )
                else if (provider.notifications.isEmpty)
                  SliverFillRemaining(
                    hasScrollBody: false,
                    child: Center(
                      child: ValoraEmptyState(
                        icon: Icons.notifications_off_outlined,
                        title: 'No notifications',
                        subtitle: "You're all caught up! Check back later for updates.",
                      ),
                    ),
                  )
                else
                  SliverPadding(
                    padding: const EdgeInsets.symmetric(vertical: 8),
                    sliver: SliverList(
                      delegate: SliverChildBuilderDelegate(
                        (context, index) {
                          if (index == provider.notifications.length) {
                            if (provider.isLoadingMore) {
                              return const Padding(
                                padding: EdgeInsets.all(16.0),
                                child: Center(child: ValoraLoadingIndicator()),
                              );
                            }
                            return const SizedBox(height: 80);
                          }

                          final notification = provider.notifications[index];
                          return _NotificationItem(
                            key: Key(notification.id),
                            notification: notification,
                            provider: provider,
                            isDark: isDark,
                            formatTime: _formatTime,
                            getIcon: _getIconForType,
                            getColor: _getColorForType,
                          );
                        },
                        childCount: provider.notifications.length + 1,
                      ),
                    ),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _NotificationItem extends StatelessWidget {
  final ValoraNotification notification;
  final NotificationService provider;
  final bool isDark;
  final String Function(DateTime) formatTime;
  final IconData Function(NotificationType) getIcon;
  final Color Function(NotificationType, bool) getColor;

  const _NotificationItem({
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
    final backgroundColor = isDark
        ? (notification.isRead ? ValoraColors.backgroundDark : ValoraColors.surfaceVariantDark.withValues(alpha: 0.3))
        : (notification.isRead ? ValoraColors.backgroundLight : ValoraColors.primary.withValues(alpha: 0.05));

    return Dismissible(
      key: Key(notification.id),
      direction: DismissDirection.endToStart,
      background: Container(
        color: ValoraColors.error,
        alignment: Alignment.centerRight,
        padding: const EdgeInsets.only(right: 24),
        child: const Icon(Icons.delete_outline_rounded, color: Colors.white),
      ),
      onDismissed: (_) {
        provider.deleteNotification(notification.id);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text('Notification deleted'),
            action: SnackBarAction(
              label: 'Undo',
              onPressed: () {
                // Ideally we would restore it, but simple delete for now
                // provider.undoDelete(); // Not implemented yet
              },
            ),
          ),
        );
      },
      child: InkWell(
        onTap: () async {
          if (!notification.isRead) {
            provider.markAsRead(notification.id);
          }
          if (notification.actionUrl != null) {
            final uri = Uri.parse(notification.actionUrl!);
            if (await canLaunchUrl(uri)) {
               await launchUrl(uri);
            }
          }
        },
        child: Container(
          color: backgroundColor,
          padding: const EdgeInsets.symmetric(
            horizontal: 24,
            vertical: 16,
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: getColor(notification.type, isDark).withValues(alpha: 0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  getIcon(notification.type),
                  color: getColor(notification.type, isDark),
                  size: 20,
                ),
              ),
              const SizedBox(width: 16),
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
                                  ? FontWeight.normal
                                  : FontWeight.bold,
                              color: isDark
                                  ? ValoraColors.neutral50
                                  : ValoraColors.neutral900,
                            ),
                          ),
                        ),
                        Text(
                          formatTime(notification.createdAt),
                          style: ValoraTypography.labelSmall.copyWith(
                            color: isDark
                                ? ValoraColors.neutral400
                                : ValoraColors.neutral500,
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
                  padding: const EdgeInsets.only(left: 12, top: 12),
                  child: Container(
                    width: 8,
                    height: 8,
                    decoration: const BoxDecoration(
                      color: ValoraColors.primary,
                      shape: BoxShape.circle,
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
