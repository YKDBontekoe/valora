import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:logging/logging.dart';
import '../models/notification.dart';
import 'api_service.dart';

class NotificationService extends ChangeNotifier {
  static final _log = Logger('NotificationService');
  ApiService _apiService;
  List<ValoraNotification> _notifications = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _offset = 0;
  static const int _pageSize = 20;
  String? _error;
  Timer? _pollingTimer;

  final Map<String, Timer> _pendingDeletions = {};
  final Map<String, (int index, ValoraNotification notification)> _pendingDeletedNotifications = {};

  NotificationService(this._apiService);


  void update(ApiService apiService) {
    _apiService = apiService;
  }

  List<ValoraNotification> get notifications => _notifications;
  int get unreadCount => _unreadCount;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasMore;
  String? get error => _error;

  void startPolling() {
    _pollingTimer?.cancel();
    _fetchUnreadCount(); // Initial fetch
    _pollingTimer = Timer.periodic(const Duration(seconds: 30), (_) {
      _fetchUnreadCount();
    });
  }

  void stopPolling() {
    _pollingTimer?.cancel();
    _pollingTimer = null;
  }

  Future<void> _fetchUnreadCount() async {
    try {
      int count = await _apiService.getUnreadNotificationCount();

      // Adjust count to account for locally pending deletes
      for (final pending in _pendingDeletedNotifications.values) {
        if (!pending.$2.isRead) {
          count = count > 0 ? count - 1 : 0;
        }
      }

      if (_unreadCount != count) {
        _unreadCount = count;
        notifyListeners();
      }
    } catch (e) {
      // Silently fail on polling errors
      _log.warning('Error polling notifications', e);
    }
  }

  Future<void> deleteNotification(String id) async {
    // If it's already pending deletion, ignore or just let it continue.
    // However, if the user triggered delete again, we shouldn't cancel the old one
    // unless we want to reset the timer. But if it's not in _notifications,
    // we can't remove it again.
    if (_pendingDeletions.containsKey(id)) {
        return;
    }

    final index = _notifications.indexWhere((n) => n.id == id);
    if (index == -1) return;

    final removed = _notifications[index];

    // Store for potential restoration
    _pendingDeletedNotifications[id] = (index, removed);

    // Optimistic update: Remove from list immediately
    _notifications.removeAt(index);
    if (!removed.isRead) {
      _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
    }
    notifyListeners();

    // Schedule API call
    _pendingDeletions[id] = Timer(const Duration(seconds: 4), () async {
      _pendingDeletions.remove(id);
      // We keep it in _pendingDeletedNotifications until success or failure is handled
      // But actually, once the timer fires, the undo window is closed.
      // So we remove it from there too. But we need a reference for restoration on failure.

      final pendingRestore = _pendingDeletedNotifications.remove(id);

      try {
        await _apiService.deleteNotification(id);
      } catch (e) {
        _log.warning('Error deleting notification', e);

        // Restore on failure
        if (pendingRestore != null) {
          final (idx, notification) = pendingRestore;
          // We need to re-insert carefully. The list might have changed (other deletions/additions).
          // Using the old index is a best-effort.
          if (idx >= 0 && idx <= _notifications.length) {
            _notifications.insert(idx, notification);
          } else {
            _notifications.add(notification);
          }
          // Restore unread count
          if (!notification.isRead) {
            _unreadCount++;
          }

          _error = "Failed to delete notification";
          notifyListeners();
        }
      }
    });
  }

  Future<void> fetchNotifications({bool refresh = false}) async {
    if (_isLoading) return;

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      // Fetch unread count in parallel with notifications
      final results = await Future.wait([
        _apiService.getNotifications(limit: _pageSize, offset: 0),
        _apiService.getUnreadNotificationCount(),
      ]);

      var fetched = results[0] as List<ValoraNotification>;
      var count = results[1] as int;

      // Filter out items that are currently pending deletion
      fetched = fetched.where((n) => !_pendingDeletions.containsKey(n.id)).toList();

      // Adjust unread count
      for (final pending in _pendingDeletedNotifications.values) {
        if (!pending.$2.isRead) {
          count = count > 0 ? count - 1 : 0;
        }
      }

      _notifications = fetched;
      _unreadCount = count;
      _hasMore = fetched.length >= _pageSize;
      _offset = fetched.length;
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMoreNotifications() async {
    if (_isLoading || _isLoadingMore || !_hasMore) return;

    _isLoadingMore = true;
    // Clear any previous error when attempting to load more
    if (_error != null) {
      _error = null;
    }
    notifyListeners();

    try {
      final fetched = await _apiService.getNotifications(limit: _pageSize, offset: _offset);

      if (fetched.isEmpty) {
        _hasMore = false;
      } else {
        _notifications.addAll(fetched);
        _hasMore = fetched.length >= _pageSize;
        _offset += fetched.length;
      }
    } catch (e) {
      _log.warning('Error loading more notifications', e);
    } finally {
      _isLoadingMore = false;
      notifyListeners();
    }
  }

  Future<void> markAsRead(String id) async {
    try {
      // Optimistic update
      final index = _notifications.indexWhere((n) => n.id == id);
      if (index != -1 && !_notifications[index].isRead) {
        final old = _notifications[index];
        _notifications[index] = ValoraNotification(
          id: old.id,
          title: old.title,
          body: old.body,
          isRead: true,
          createdAt: old.createdAt,
          type: old.type,
          actionUrl: old.actionUrl,
        );
        _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
        notifyListeners();

        await _apiService.markNotificationAsRead(id);
      }
    } catch (e) {
      _log.warning('Error marking notification as read', e);
      // Revert if needed, but for read status it's usually fine
    }
  }

  Future<void> markAllAsRead() async {
    try {
      // Optimistic update
      _notifications = _notifications.map((n) => ValoraNotification(
        id: n.id,
        title: n.title,
        body: n.body,
        isRead: true,
        createdAt: n.createdAt,
        type: n.type,
        actionUrl: n.actionUrl,
      )).toList();
      _unreadCount = 0;
      notifyListeners();

      await _apiService.markAllNotificationsAsRead();
    } catch (e) {
      _log.warning('Error marking all notifications as read', e);
    }
  }

  void undoDelete(String id) {
    if (!_pendingDeletions.containsKey(id)) return;

    // Cancel deletion
    _pendingDeletions[id]?.cancel();
    _pendingDeletions.remove(id);

    // Restore notification
    if (_pendingDeletedNotifications.containsKey(id)) {
      final (index, notification) = _pendingDeletedNotifications[id]!;
      _pendingDeletedNotifications.remove(id);

      if (index >= 0 && index <= _notifications.length) {
        _notifications.insert(index, notification);
      } else {
        _notifications.add(notification);
      }

      if (!notification.isRead) {
        _unreadCount++;
      }
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    for (final timer in _pendingDeletions.values) {
      timer.cancel();
    }
    _pendingDeletions.clear();
    _pendingDeletedNotifications.clear();
    super.dispose();
  }
}
