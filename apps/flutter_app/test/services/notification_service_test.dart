import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:fake_async/fake_async.dart';

@GenerateMocks([ApiService])
import 'notification_service_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late NotificationService notificationService;

  setUp(() {
    mockApiService = MockApiService();
    notificationService = NotificationService(mockApiService);

    // Default mock for unread count
    when(mockApiService.getUnreadNotificationCount())
        .thenAnswer((_) async => 0);
  });

  group('NotificationService', () {
    test('fetchNotifications updates list and clears error', () async {
      final notifications = [
        ValoraNotification(
          id: '1',
          title: 'Test',
          body: 'Body',
          isRead: false,
          createdAt: DateTime.now(),
          type: NotificationType.info,
        ),
      ];

      when(mockApiService.getNotifications(limit: 20, offset: 0))
          .thenAnswer((_) async => notifications);
      when(mockApiService.getUnreadNotificationCount())
          .thenAnswer((_) async => 5);

      await notificationService.fetchNotifications();

      expect(notificationService.notifications, equals(notifications));
      expect(notificationService.unreadCount, 5);
      expect(notificationService.isLoading, isFalse);
      expect(notificationService.error, isNull);
    });

    test('fetchNotifications handles error', () async {
      when(mockApiService.getNotifications(limit: 20, offset: 0))
          .thenThrow(Exception('Network error'));

      await notificationService.fetchNotifications();

      expect(notificationService.notifications, isEmpty);
      expect(notificationService.isLoading, isFalse);
      expect(notificationService.error, contains('Network error'));
    });

    test('markAsRead updates state optimistically and calls API', () async {
      final notification = ValoraNotification(
        id: '1',
        title: 'Test',
        body: 'Body',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      );

      // Setup initial state
      when(mockApiService.getNotifications(limit: 20, offset: 0))
          .thenAnswer((_) async => [notification]);
      await notificationService.fetchNotifications();

      // Setup mark call
      when(mockApiService.markNotificationAsRead('1'))
          .thenAnswer((_) async => {});

      await notificationService.markAsRead('1');

      expect(notificationService.notifications.first.isRead, isTrue);
      verify(mockApiService.markNotificationAsRead('1')).called(1);
    });

    test('markAllAsRead updates all items optimistically and calls API', () async {
      final notifications = [
        ValoraNotification(
          id: '1',
          title: 'Test 1',
          body: 'Body',
          isRead: false,
          createdAt: DateTime.now(),
          type: NotificationType.info,
        ),
        ValoraNotification(
          id: '2',
          title: 'Test 2',
          body: 'Body',
          isRead: true,
          createdAt: DateTime.now(),
          type: NotificationType.info,
        ),
      ];

      when(mockApiService.getNotifications(limit: 20, offset: 0))
          .thenAnswer((_) async => notifications);
      await notificationService.fetchNotifications();

      when(mockApiService.markAllNotificationsAsRead())
          .thenAnswer((_) async => {});

      await notificationService.markAllAsRead();

      expect(notificationService.notifications.every((n) => n.isRead), isTrue);
      expect(notificationService.unreadCount, 0);
      verify(mockApiService.markAllNotificationsAsRead()).called(1);
    });

    test('deleteNotification updates list optimistically and calls API after delay', () {
      fakeAsync((async) {
        final notification = ValoraNotification(
          id: '1',
          title: 'Test 1',
          body: 'Body',
          isRead: false,
          createdAt: DateTime.now(),
          type: NotificationType.info,
        );

        // Setup initial state
        when(mockApiService.getNotifications(limit: 20, offset: 0))
            .thenAnswer((_) async => [notification]);
        when(mockApiService.deleteNotification('1'))
            .thenAnswer((_) async {});

        // We can't use await inside fakeAsync for the initial fetch if it involves real Futures not controlled by fakeAsync.
        // But mockApiService returns Futures.
        notificationService.fetchNotifications();
        async.flushMicrotasks();

        expect(notificationService.notifications, hasLength(1));

        notificationService.deleteNotification('1');
        async.flushMicrotasks();

        // Optimistic update
        expect(notificationService.notifications, isEmpty);

        // API call should NOT have happened yet
        verifyNever(mockApiService.deleteNotification('1'));

        // Fast forward 4 seconds
        async.elapse(const Duration(seconds: 4));

        // API call should have happened
        verify(mockApiService.deleteNotification('1')).called(1);
      });
    });

    test('undoDelete restores notification and cancels API call', () {
      fakeAsync((async) {
        final notification = ValoraNotification(
          id: '1',
          title: 'Test 1',
          body: 'Body',
          isRead: false,
          createdAt: DateTime.now(),
          type: NotificationType.info,
        );

        when(mockApiService.getNotifications(limit: 20, offset: 0))
            .thenAnswer((_) async => [notification]);

        notificationService.fetchNotifications();
        async.flushMicrotasks();

        // Delete
        notificationService.deleteNotification('1');
        async.flushMicrotasks();
        expect(notificationService.notifications, isEmpty);

        // Undo before timer expires
        async.elapse(const Duration(seconds: 2));
        notificationService.undoDelete('1');
        async.flushMicrotasks();

        // Should be restored
        expect(notificationService.notifications, hasLength(1));
        expect(notificationService.notifications.first.id, '1');

        // Fast forward past original timer
        async.elapse(const Duration(seconds: 3));

        // API call should NEVER happen
        verifyNever(mockApiService.deleteNotification('1'));
      });
    });

    test('fetchNotifications filters out pending deletions', () {
       fakeAsync((async) {
        final notification = ValoraNotification(
          id: '1',
          title: 'Test 1',
          body: 'Body',
          isRead: false,
          createdAt: DateTime.now(),
          type: NotificationType.info,
        );

        when(mockApiService.getNotifications(limit: 20, offset: 0))
            .thenAnswer((_) async => [notification]);
        when(mockApiService.deleteNotification('1'))
            .thenAnswer((_) async {});

        notificationService.fetchNotifications();
        async.flushMicrotasks();
        expect(notificationService.notifications, hasLength(1));

        // Delete
        notificationService.deleteNotification('1');
        async.flushMicrotasks();
        expect(notificationService.notifications, isEmpty);

        // Simulate a background fetch that returns the item again (because API delete hasn't happened yet)
        when(mockApiService.getNotifications(limit: 20, offset: 0))
            .thenAnswer((_) async => [notification]);

        notificationService.fetchNotifications();
        async.flushMicrotasks();

        // Should STILL be empty because it's pending delete
        expect(notificationService.notifications, isEmpty);

        // Wait for timer to complete
        async.elapse(const Duration(seconds: 4));
        verify(mockApiService.deleteNotification('1')).called(1);
      });
    });
  });
}
