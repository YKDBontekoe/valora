import 'dart:async';
import 'package:flutter/foundation.dart';
import '../models/notification.dart';
import 'api_service.dart';

class NotificationService extends ChangeNotifier {
  ApiService _apiService;
  List<ValoraNotification> _notifications = [];
  final Map<String, ValoraNotification> _pendingDeletions = {};
  final Map<String, Timer> _deletionTimers = {};

  int _unreadCount = 0;
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _offset = 0;
  static const int _pageSize = 20;
  String? _error;
  Timer? _pollingTimer;

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
      final count = await _apiService.getUnreadNotificationCount();
      if (_unreadCount != count) {
        _unreadCount = count;
        notifyListeners();
      }
    } catch (e) {
      // Silently fail on polling errors
      if (kDebugMode) {
        print('Error polling notifications: $e');
      }
    }
  }

  Future<void> deleteNotification(String id) async {
    final index = _notifications.indexWhere((n) => n.id == id);
    if (index == -1) {
        // Already deleted or pending
        if (_pendingDeletions.containsKey(id)) {
            // Reset timer if re-deleted while pending
             _deletionTimers[id]?.cancel();
             _deletionTimers[id] = Timer(const Duration(seconds: 4), () => _commitDelete(id));
        }
        return;
    }

    final removed = _notifications[index];

    // Store in pending deletions map (deduplicated by key)
    _pendingDeletions[id] = removed;

    // Optimistic update
    _notifications.removeAt(index);
    if (!removed.isRead) {
      _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
    }
    notifyListeners();

    // Start timer for actual deletion
    _deletionTimers[id]?.cancel();
    _deletionTimers[id] = Timer(const Duration(seconds: 4), () => _commitDelete(id));
  }

  Future<void> _commitDelete(String id) async {
      _deletionTimers.remove(id);
      _pendingDeletions.remove(id);

      try {
        await _apiService.deleteNotification(id);
      } catch (e) {
        if (kDebugMode) {
          print('Error deleting notification: $e');
        }
        // Failure is silently ignored for user experience, but logged.
        // The item remains deleted locally for the session.
      }
  }

  void undoDelete(String id) {
    if (!_pendingDeletions.containsKey(id)) return;

    final notification = _pendingDeletions[id]!;

    // Cancel timer
    _deletionTimers[id]?.cancel();
    _deletionTimers.remove(id);

    // Remove from pending
    _pendingDeletions.remove(id);

    // Add back to notifications
    _notifications.add(notification);

    // Sort to keep order correct
    _notifications.sort((a, b) => b.createdAt.compareTo(a.createdAt));

    if (!notification.isRead) {
      _unreadCount++;
    }
    notifyListeners();
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
      final count = results[1] as int;

      // Filter out pending deletions so they don't reappear
      if (_pendingDeletions.isNotEmpty) {
          fetched = fetched.where((n) => !_pendingDeletions.containsKey(n.id)).toList();
      }

      _notifications = fetched;
      _unreadCount = count;
      _hasMore = fetched.length >= _pageSize;
      _offset = fetched.length;

      // Clear pending deletions on fresh fetch ONLY if refresh is explicit,
      // but usually we want to respect the pending state even across refreshes within the same session.
      // However, if the user explicitly refreshes, maybe they want the truth from server?
      // Let's stick to filtering them out to keep the UI consistent with user actions.

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
      var fetched = await _apiService.getNotifications(limit: _pageSize, offset: _offset);

      // Filter out pending deletions
      if (_pendingDeletions.isNotEmpty) {
          fetched = fetched.where((n) => !_pendingDeletions.containsKey(n.id)).toList();
      }

      if (fetched.isEmpty) {
        _hasMore = false;
      } else {
        _notifications.addAll(fetched);
        _hasMore = fetched.length >= _pageSize;
        _offset += fetched.length;
      }
    } catch (e) {
      if (kDebugMode) {
        print('Error loading more notifications: $e');
      }
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
      if (kDebugMode) {
        print('Error marking notification as read: $e');
      }
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
      if (kDebugMode) {
        print('Error marking all notifications as read: $e');
      }
    }
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    for (var timer in _deletionTimers.values) {
        timer.cancel();
    }
    super.dispose();
  }
}
