import 'package:flutter/foundation.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/models/notification.dart';

class MockNotificationService extends ChangeNotifier implements NotificationService {
  List<ValoraNotification> _notifications = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  String? _error;

  @override
  List<ValoraNotification> get notifications => _notifications;

  @override
  int get unreadCount => _unreadCount;

  @override
  bool get isLoading => _isLoading;

  @override
  bool get isLoadingMore => _isLoadingMore;

  @override
  bool get hasMore => _hasMore;

  @override
  String? get error => _error;

  // Setters for testing
  void setNotifications(List<ValoraNotification> value) {
    _notifications = value;
    notifyListeners();
  }

  void setUnreadCount(int value) {
    _unreadCount = value;
    notifyListeners();
  }

  void setIsLoading(bool value) {
    _isLoading = value;
    notifyListeners();
  }

  void setIsLoadingMore(bool value) {
    _isLoadingMore = value;
    notifyListeners();
  }

  void setHasMore(bool value) {
    _hasMore = value;
    notifyListeners();
  }

  void setError(String? value) {
    _error = value;
    notifyListeners();
  }

  @override
  Future<void> fetchNotifications({bool refresh = false}) async {}

  @override
  Future<void> loadMoreNotifications() async {}

  @override
  Future<void> markAsRead(String id) async {}

  @override
  Future<void> markAllAsRead() async {}

  @override
  Future<void> deleteNotification(String id) async {
    _notifications = _notifications.where((n) => n.id != id).toList();
    notifyListeners();
  }

  @override
  void startPolling() {}

  @override
  void stopPolling() {}

  @override
  void update(dynamic apiService) {}

  @override
  void undoDelete(String id) {
    notifyListeners();
  }

  @override
  void handleNotificationCreated(List<Object?>? args) {
    if (args != null && args.isNotEmpty) {
      try {
        final map = args[0] as Map<String, dynamic>;
        final notification = ValoraNotification.fromJson(map);
        if (!_notifications.any((n) => n.id == notification.id)) {
          _notifications.insert(0, notification);
          _unreadCount++;
          notifyListeners();
        }
      } catch (_) {
        // Ignore
      }
    }
  }

  @override
  void handleNotificationRead(List<Object?>? args) {
    if (args != null && args.isNotEmpty) {
      final id = args[0] as String;
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
      }
    }
  }

  @override
  void handleNotificationDeleted(List<Object?>? args) {
    if (args != null && args.isNotEmpty) {
      final id = args[0] as String;
      final index = _notifications.indexWhere((n) => n.id == id);
      if (index != -1) {
        final removed = _notifications[index];
        _notifications.removeAt(index);
        if (!removed.isRead) {
          _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
        }
        notifyListeners();
      }
    }
  }
}
