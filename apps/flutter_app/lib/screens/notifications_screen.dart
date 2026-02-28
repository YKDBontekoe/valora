import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
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
        provider.hasMore) {
      provider.loadMoreNotifications();
    }
  }

  Future<void> _handleRefresh() async {
    await context.read<NotificationService>().fetchNotifications(refresh: true);
  }

  Future<void> _confirmMarkAllRead() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Mark all as read?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Confirm',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text(
          'Are you sure you want to mark all notifications as read?',
        ),
      ),
    );

    if (confirmed == true && mounted) {
      context.read<NotificationService>().markAllAsRead();
    }
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
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _handleRefresh,
        child: CustomScrollView(
          controller: _scrollController,
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            Selector<NotificationService, int>(
              selector: (_, p) => p.unreadCount,
              builder: (context, unreadCount, _) {
                return SliverAppBar(
                  pinned: true,
                  backgroundColor:
                      colorScheme.surface.withValues(alpha: 0.95),
                  surfaceTintColor: Colors.transparent,
                  automaticallyImplyLeading: false,
                  title: Row(
                    children: [
                      Text(
                        'Alerts',
                        style: ValoraTypography.headlineMedium.copyWith(
                          color: colorScheme.onSurface,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      if (unreadCount > 0) ...[
                        const SizedBox(width: 10),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 8, vertical: 2),
                          decoration: BoxDecoration(
                            color: ValoraColors.primary,
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Text(
                            '$unreadCount',
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ],
                    ],
                  ),
                  centerTitle: false,
                  actions: [
                    if (unreadCount > 0)
                      TextButton.icon(
                        onPressed: _confirmMarkAllRead,
                        icon: const Icon(Icons.done_all_rounded, size: 18),
                        label: const Text('Read all'),
                        style: TextButton.styleFrom(
                          foregroundColor: ValoraColors.primary,
                        ),
                      ),
                    const SizedBox(width: 8),
                  ],
                );
              },
            ),
            Consumer<NotificationService>(
              builder: (context, provider, _) {
                if (provider.isLoading && provider.notifications.isEmpty) {
                  return const SliverFillRemaining(
                    child: Center(
                      child: ValoraLoadingIndicator(
                          message: 'Loading notifications...'),
                    ),
                  );
                }

                if (provider.error != null && provider.notifications.isEmpty) {
                  return SliverFillRemaining(
                    hasScrollBody: false,
                    child: Center(
                      child: ValoraEmptyState(
                        icon: Icons.error_outline_rounded,
                        title: 'Something went wrong',
                        subtitle:
                            'We couldn\'t load your notifications. Please try again.',
                        actionLabel: 'Retry',
                        onAction: _handleRefresh,
                      ),
                    ),
                  );
                }

                if (provider.notifications.isEmpty) {
                  return SliverFillRemaining(
                    hasScrollBody: false,
                    child: Center(
                      child: Padding(
                        padding: const EdgeInsets.only(bottom: 80),
                        child: ValoraEmptyState(
                          icon: Icons.notifications_active_rounded,
                          title: 'All caught up!',
                          subtitle:
                              "No new alerts right now.\nWe'll notify you about price drops and new listings.",
                        ),
                      ),
                    ),
                  );
                }

                return SliverPadding(
                  padding: const EdgeInsets.symmetric(
                      vertical: ValoraSpacing.sm,
                      horizontal: ValoraSpacing.md),
                  sliver: SliverList(
                    delegate: SliverChildBuilderDelegate(
                      (context, index) {
                        if (index == provider.notifications.length) {
                          if (provider.isLoadingMore) {
                            return const Padding(
                              padding: EdgeInsets.all(16.0),
                              child:
                                  Center(child: ValoraLoadingIndicator()),
                            );
                          }
                          return const SizedBox(height: 100);
                        }

                        final notification = provider.notifications[index];
                        return Padding(
                          padding: const EdgeInsets.only(
                              bottom: ValoraSpacing.sm),
                          child: _NotificationCard(
                            key: Key(notification.id),
                            notification: notification,
                            provider: provider,
                            isDark: isDark,
                            formatTime: _formatTime,
                            getIcon: _getIconForType,
                            getColor: _getColorForType,
                          ),
                        );
                      },
                      childCount: provider.notifications.length + 1,
                    ),
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _NotificationCard extends StatelessWidget {
  final ValoraNotification notification;
  final NotificationService provider;
  final bool isDark;
  final String Function(DateTime) formatTime;
  final IconData Function(NotificationType) getIcon;
  final Color Function(NotificationType, bool) getColor;

  const _NotificationCard({
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
            final uri = Uri.parse(notification.actionUrl!);
            if (await canLaunchUrl(uri)) {
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
