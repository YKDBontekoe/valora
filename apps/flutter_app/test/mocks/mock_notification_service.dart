import 'package:flutter/foundation.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/models/notification.dart';

class MockNotificationService extends ChangeNotifier implements NotificationService {
  List<ValoraNotification> _notifications = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  String? _error;

  @override
  List<ValoraNotification> get notifications => _notifications;

  @override
  int get unreadCount => _unreadCount;

  @override
  bool get isLoading => _isLoading;

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

  void setError(String? value) {
    _error = value;
    notifyListeners();
  }

  @override
  Future<void> fetchNotifications({bool refresh = false}) async {}

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
}
