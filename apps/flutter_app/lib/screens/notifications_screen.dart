import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../services/notification_service.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/notifications/notification_list.dart';

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
    // No listener here, handled by NotificationList via controller

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

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

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
                    if (unreadCount > 0)
                      TextButton.icon(
                        onPressed: _confirmMarkAllRead,
                        icon: const Icon(Icons.done_all_rounded, size: 18),
                        label: const Text('Mark all read'),
                        style: TextButton.styleFrom(
                          foregroundColor: ValoraColors.primary,
                        ),
                      ),
                    const SizedBox(width: 8),
                  ],
                );
              }
            ),
            NotificationList(
              scrollController: _scrollController,
              asSliver: true,
            ),
          ],
        ),
      ),
    );
  }
}
