import 'package:flutter/foundation.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/models/notification.dart';

class MockNotificationService extends ChangeNotifier implements NotificationService {
  @override
  List<ValoraNotification> get notifications => [];

  @override
  int get unreadCount => 0;

  @override
  bool get isLoading => false;

  @override
  String? get error => null;

  @override
  Future<void> fetchNotifications({bool refresh = false}) async {}

  @override
  Future<void> markAsRead(String id) async {}

  @override
  Future<void> markAllAsRead() async {}

  @override
  void startPolling() {}

  @override
  void stopPolling() {}

  @override
  void update(dynamic apiService) {}
}
