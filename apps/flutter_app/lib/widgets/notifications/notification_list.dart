import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../models/notification.dart';
import '../../core/theme/valora_colors.dart';
import '../../services/notification_service.dart';
import '../valora_widgets.dart';
import 'notification_item.dart';

class NotificationList extends StatefulWidget {
  final ScrollController? scrollController;
  final bool shrinkWrap;
  final Widget Function(BuildContext, int)? separatorBuilder;
  final bool asSliver;

  const NotificationList({
    super.key,
    this.scrollController,
    this.shrinkWrap = false,
    this.separatorBuilder,
    this.asSliver = false,
  });

  @override
  State<NotificationList> createState() => _NotificationListState();
}

class _NotificationListState extends State<NotificationList> {
  late ScrollController _scrollController;
  bool _isInternalController = false;

  @override
  void initState() {
    super.initState();
    if (widget.scrollController != null) {
      _scrollController = widget.scrollController!;
    } else {
      // For slivers, usually the controller belongs to CustomScrollView.
      // If asSliver is true, we might not want to attach a listener here if the controller is passed?
      // Actually we do want to listen for pagination.
      _scrollController = ScrollController();
      _isInternalController = true;
    }
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    if (_isInternalController) {
      _scrollController.dispose();
    }
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.hasClients &&
        _scrollController.position.pixels >=
            _scrollController.position.maxScrollExtent - 200) {
      context.read<NotificationService>().loadMoreNotifications();
    }
  }

  Future<void> _handleRefresh() async {
    await context.read<NotificationService>().fetchNotifications(refresh: true);
  }

  Widget _buildItem(BuildContext context, ValoraNotification notification, NotificationService provider) {
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
                onPressed: () => provider.undoDelete(notification.id),
              ),
            ),
          );
        },
        child: NotificationItem(
          notification: notification,
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
        ),
      );
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<NotificationService>(
      builder: (context, provider, _) {
        if (provider.isLoading && provider.notifications.isEmpty) {
          if (widget.asSliver) {
            return const SliverFillRemaining(
              child: Center(child: ValoraLoadingIndicator(message: 'Loading notifications...')),
            );
          }
          return const Center(child: ValoraLoadingIndicator(message: 'Loading notifications...'));
        }

        if (provider.error != null && provider.notifications.isEmpty) {
          final content = Center(
            child: ValoraEmptyState(
              icon: Icons.error_outline_rounded,
              title: 'Something went wrong',
              subtitle: 'We couldn\'t load your notifications. Please try again.',
              actionLabel: 'Retry',
              onAction: _handleRefresh,
            ),
          );

          if (widget.asSliver) {
             return SliverFillRemaining(hasScrollBody: false, child: content);
          }
          return LayoutBuilder(
            builder: (context, constraints) {
              return SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                child: SizedBox(
                  height: constraints.maxHeight,
                  child: content,
                ),
              );
            }
          );
        }

        if (provider.notifications.isEmpty) {
          final content = Center(
            child: ValoraEmptyState(
              icon: Icons.notifications_off_outlined,
              title: 'No notifications',
              subtitle: "You're all caught up! Check back later for updates.",
            ),
          );

          if (widget.asSliver) {
             return SliverFillRemaining(hasScrollBody: false, child: content);
          }
          return LayoutBuilder(
            builder: (context, constraints) {
               return RefreshIndicator(
                  onRefresh: _handleRefresh,
                  child: SingleChildScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    child: SizedBox(
                        height: constraints.maxHeight,
                        child: content,
                    ),
                  ),
               );
            }
          );
        }

        if (widget.asSliver) {
            return SliverList(
                delegate: SliverChildBuilderDelegate(
                    (context, index) {
                        if (index == provider.notifications.length) {
                            return const Padding(
                                padding: EdgeInsets.all(16.0),
                                child: Center(child: ValoraLoadingIndicator()),
                            );
                        }
                        return _buildItem(context, provider.notifications[index], provider);
                    },
                    childCount: provider.notifications.length + (provider.isLoadingMore ? 1 : 0),
                ),
            );
        }

        return RefreshIndicator(
          onRefresh: _handleRefresh,
          child: ListView.separated(
            controller: _scrollController,
            physics: const AlwaysScrollableScrollPhysics(),
            shrinkWrap: widget.shrinkWrap,
            itemCount: provider.notifications.length + (provider.isLoadingMore ? 1 : 0),
            separatorBuilder: widget.separatorBuilder ?? (context, index) => const Divider(height: 1, indent: 72),
            itemBuilder: (context, index) {
              if (index == provider.notifications.length) {
                return const Padding(
                  padding: EdgeInsets.all(16.0),
                  child: Center(child: ValoraLoadingIndicator()),
                );
              }
              return _buildItem(context, provider.notifications[index], provider);
            },
          ),
        );
      },
    );
  }
}
