import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/notification_service.dart';

@GenerateMocks([ApiService])
import 'notification_service_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late NotificationService notificationService;

  setUp(() {
    mockApiService = MockApiService();
    notificationService = NotificationService(mockApiService);
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

      when(mockApiService.getNotifications(limit: 50))
          .thenAnswer((_) async => notifications);

      await notificationService.fetchNotifications();

      expect(notificationService.notifications, equals(notifications));
      expect(notificationService.isLoading, isFalse);
      expect(notificationService.error, isNull);
    });

    test('fetchNotifications handles error', () async {
      when(mockApiService.getNotifications(limit: 50))
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
      when(mockApiService.getNotifications(limit: 50))
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

      when(mockApiService.getNotifications(limit: 50))
          .thenAnswer((_) async => notifications);
      await notificationService.fetchNotifications();

      when(mockApiService.markAllNotificationsAsRead())
          .thenAnswer((_) async => {});

      await notificationService.markAllAsRead();

      expect(notificationService.notifications.every((n) => n.isRead), isTrue);
      expect(notificationService.unreadCount, 0);
      verify(mockApiService.markAllNotificationsAsRead()).called(1);
    });

    test('deleteNotification updates list optimistically and calls API', () async {
      final notification = ValoraNotification(
        id: '1',
        title: 'Test 1',
        body: 'Body',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      );

      // Setup initial state
      when(mockApiService.getNotifications(limit: 50))
          .thenAnswer((_) async => [notification]);
      await notificationService.fetchNotifications();

      // Setup delete call
      when(mockApiService.deleteNotification('1'))
          .thenAnswer((_) async {});

      await notificationService.deleteNotification('1');

      expect(notificationService.notifications, isEmpty);
      verify(mockApiService.deleteNotification('1')).called(1);
    });
  });
}
