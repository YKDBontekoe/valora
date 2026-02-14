import 'dart:async';
import 'package:fake_async/fake_async.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/models/notification.dart';

// Manual Mock
class MockApiService extends Fake implements ApiService {
  int _unreadCount = 0;
  List<ValoraNotification> _notifications = [];
  final List<String> deletedIds = [];

  // setters for test setup
  set unreadCount(int value) => _unreadCount = value;
  set notifications(List<ValoraNotification> value) => _notifications = value;

  @override
  Future<int> getUnreadNotificationCount() async => _unreadCount;

  @override
  Future<List<ValoraNotification>> getNotifications({bool unreadOnly = false, int limit = 50, int offset = 0}) async {
    return _notifications;
  }

  @override
  Future<void> deleteNotification(String id) async {
    deletedIds.add(id);
  }

  @override
  Future<void> markNotificationAsRead(String id) async {}

  @override
  Future<void> markAllNotificationsAsRead() async {}
}

void main() {
  test('deleteNotification removes item immediately and calls API after delay', () {
    fakeAsync((async) {
      // 1. Setup
      final mockApiService = MockApiService();
      final service = NotificationService(mockApiService);

      final notification = ValoraNotification(
        id: '1',
        title: 'Test',
        body: 'Body',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      );
      mockApiService.notifications = [notification];
      mockApiService.unreadCount = 1;

      // Initialize service data
      service.fetchNotifications();
      async.flushMicrotasks();

      expect(service.notifications.length, 1);
      expect(service.unreadCount, 1);

      // 2. Delete
      service.deleteNotification('1');

      // 3. Verify optimistic removal
      expect(service.notifications.isEmpty, true);
      expect(service.unreadCount, 0);
      expect(mockApiService.deletedIds.isEmpty, true); // API not called yet

      // 4. Fast forward time < 4 seconds
      async.elapse(const Duration(seconds: 3));
      expect(mockApiService.deletedIds.isEmpty, true); // Still not called

      // 5. Fast forward past 4 seconds
      async.elapse(const Duration(seconds: 2)); // Total 5s
      expect(mockApiService.deletedIds, contains('1')); // API called
    });
  });

  test('undoDelete restores item and prevents API call', () {
    fakeAsync((async) {
      // 1. Setup
      final mockApiService = MockApiService();
      final service = NotificationService(mockApiService);

      final notification = ValoraNotification(
        id: '1',
        title: 'Test',
        body: 'Body',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      );
      mockApiService.notifications = [notification];
      mockApiService.unreadCount = 1;

      service.fetchNotifications();
      async.flushMicrotasks();

      // 2. Delete
      service.deleteNotification('1');
      expect(service.notifications.isEmpty, true);

      // 3. Undo before timeout
      async.elapse(const Duration(seconds: 2));
      service.undoDelete('1');

      // 4. Verify restoration
      expect(service.notifications.length, 1);
      expect(service.notifications.first.id, '1');
      expect(service.unreadCount, 1);
      expect(mockApiService.deletedIds.isEmpty, true);

      // 5. Fast forward past original timeout
      async.elapse(const Duration(seconds: 5));
      expect(mockApiService.deletedIds.isEmpty, true); // API never called
    });
  });
}
