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
  bool shouldFailDelete = false;

  // setters for test setup
  set unreadCount(int value) => _unreadCount = value;
  set notifications(List<ValoraNotification> value) => _notifications = value;

  @override
  Future<int> getUnreadNotificationCount() async => _unreadCount;

  @override
  Future<List<ValoraNotification>> getNotifications({
    bool unreadOnly = false,
    int limit = 50,
    int offset = 0,
  }) async {
    return _notifications;
  }

  @override
  Future<void> deleteNotification(String id) async {
    if (shouldFailDelete) {
      throw Exception('Network error');
    }
    deletedIds.add(id);
  }

  @override
  Future<void> markNotificationAsRead(String id) async {}

  @override
  Future<void> markAllNotificationsAsRead() async {}
}

void main() {
  test(
    'deleteNotification removes item immediately and calls API after delay',
    () {
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
    },
  );

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

  test('double delete is ignored', () {
    fakeAsync((async) {
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
      service.fetchNotifications();
      async.flushMicrotasks();

      service.deleteNotification('1');
      service.deleteNotification('1'); // Should be ignored

      expect(service.notifications.isEmpty, true);
      async.elapse(const Duration(seconds: 5));
      expect(mockApiService.deletedIds.length, 1); // Only called once
    });
  });

  test('fetchNotifications excludes pending deletions', () {
    fakeAsync((async) {
      final mockApiService = MockApiService();
      final service = NotificationService(mockApiService);

      final n1 = ValoraNotification(
        id: '1',
        title: 'Test',
        body: 'Body',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      );
      final n2 = ValoraNotification(
        id: '2',
        title: 'Test 2',
        body: 'Body',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      );

      mockApiService.notifications = [n1, n2];
      mockApiService.unreadCount = 2;

      service.fetchNotifications();
      async.flushMicrotasks();

      // Delete n1 locally
      service.deleteNotification('1');
      expect(service.notifications.length, 1);
      expect(service.unreadCount, 1);

      // Fetch again from API (which still has n1 because timer hasn't fired)
      service.fetchNotifications();
      async.flushMicrotasks();

      // Should still be hidden
      expect(service.notifications.length, 1);
      expect(service.notifications.first.id, '2');
      expect(service.unreadCount, 1); // Corrected count
    });
  });

  test('delete failure restores notification', () {
    fakeAsync((async) {
      final mockApiService = MockApiService();
      mockApiService.shouldFailDelete = true;
      final service = NotificationService(mockApiService);

      final n1 = ValoraNotification(
        id: '1',
        title: 'Test',
        body: 'Body',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      );
      mockApiService.notifications = [n1];

      service.fetchNotifications();
      async.flushMicrotasks();

      service.deleteNotification('1');
      expect(service.notifications.isEmpty, true);

      // Wait for timer and failure
      async.elapse(const Duration(seconds: 5));

      // Should be restored
      expect(service.notifications.length, 1);
      expect(service.notifications.first.id, '1');
      expect(service.error, isNotNull);
    });
  });
}
