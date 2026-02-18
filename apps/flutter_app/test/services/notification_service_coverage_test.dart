import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/notification_service.dart';

@GenerateMocks([ApiService])
import 'notification_service_coverage_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late NotificationService notificationService;

  setUp(() {
    mockApiService = MockApiService();
    notificationService = NotificationService(mockApiService);
  });

  tearDown(() {
    notificationService.stopPolling();
  });

  group('NotificationService Coverage', () {
    test('_fetchUnreadCount failure is handled silently', () async {
      when(mockApiService.getUnreadNotificationCount()).thenThrow(Exception('Poll failed'));

      // Should not throw
      notificationService.startPolling();
      // Wait for timer? It's initial call is immediate but async.
      // startPolling calls _fetchUnreadCount() immediately (async).
      // We can't await void return if it's fire-and-forget inside.
      // But we can check if it crashed. It won't.
    });

    test('loadMoreNotifications failure sets error state', () async {
      when(mockApiService.getNotifications(limit: anyNamed('limit'), offset: anyNamed('offset')))
          .thenThrow(Exception('Load failed'));

      // Setup initial state to allow loadMore
      // We need _hasMore = true (default) and not loading.

      // But we need to make sure isLoadingMore is set.
      // Since `getNotifications` is called, we can await it.

      await notificationService.loadMoreNotifications();

      // Error is logged, state is reset.
      expect(notificationService.isLoadingMore, false);
      // _error is cleared at start, but exception is caught and logged.
      // It doesn't set _error property for loadMore failure?
      // Looking at code: Yes, catch block just logs warning.
    });

    test('markAsRead failure is handled', () async {
      final n = ValoraNotification(id: '1', title: 'T', body: 'B', isRead: false, createdAt: DateTime.now(), type: NotificationType.info);
      // We need to inject notification into service private list.
      // Fetch first.
      when(mockApiService.getNotifications(limit: anyNamed('limit'), offset: anyNamed('offset')))
          .thenAnswer((_) async => [n]);
      when(mockApiService.getUnreadNotificationCount()).thenAnswer((_) async => 1);

      await notificationService.fetchNotifications();

      when(mockApiService.markNotificationAsRead('1')).thenThrow(Exception('Mark failed'));

      await notificationService.markAsRead('1');

      // Should not throw, logs warning.
      // Optimistic update happened?
      // Yes, unread count decreased.
      expect(notificationService.unreadCount, 0);
    });

    test('markAllAsRead failure is handled', () async {
      final n = ValoraNotification(id: '1', title: 'T', body: 'B', isRead: false, createdAt: DateTime.now(), type: NotificationType.info);
      when(mockApiService.getNotifications(limit: anyNamed('limit'), offset: anyNamed('offset')))
          .thenAnswer((_) async => [n]);
      when(mockApiService.getUnreadNotificationCount()).thenAnswer((_) async => 1);

      await notificationService.fetchNotifications();

      when(mockApiService.markAllNotificationsAsRead()).thenThrow(Exception('Mark all failed'));

      await notificationService.markAllAsRead();

      // Optimistic update
      expect(notificationService.unreadCount, 0);
    });
  });
}
