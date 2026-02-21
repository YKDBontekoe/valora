import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/models/cursor_paged_result.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/notification_service.dart';

@GenerateMocks([ApiService])
import 'notification_service_coverage_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late NotificationService notificationService;

  setUp(() {
    mockApiService = MockApiService();
    // Default auth token for SignalR check
    when(mockApiService.authToken).thenReturn('test-token');
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
    });

    test('loadMoreNotifications failure sets error state', () async {
      // Corrected: use cursor, not offset
      when(mockApiService.getNotifications(limit: anyNamed('limit'), cursor: anyNamed('cursor')))
          .thenThrow(Exception('Load failed'));

      await notificationService.loadMoreNotifications();

      // Error is logged, state is reset.
      expect(notificationService.isLoadingMore, false);
    });

    test('markAsRead failure is handled', () async {
      final n = ValoraNotification(id: '1', title: 'T', body: 'B', isRead: false, createdAt: DateTime.now(), type: NotificationType.info);

      // Return CursorPagedResult
      final result = CursorPagedResult<ValoraNotification>(items: [n], hasMore: false);

      when(mockApiService.getNotifications(limit: anyNamed('limit'), cursor: anyNamed('cursor')))
          .thenAnswer((_) async => result);
      when(mockApiService.getUnreadNotificationCount()).thenAnswer((_) async => 1);

      await notificationService.fetchNotifications();

      when(mockApiService.markNotificationAsRead('1')).thenThrow(Exception('Mark failed'));

      await notificationService.markAsRead('1');

      // Optimistic update happened
      expect(notificationService.unreadCount, 0);
    });

    test('markAllAsRead failure is handled', () async {
      final n = ValoraNotification(id: '1', title: 'T', body: 'B', isRead: false, createdAt: DateTime.now(), type: NotificationType.info);
      final result = CursorPagedResult<ValoraNotification>(items: [n], hasMore: false);

      when(mockApiService.getNotifications(limit: anyNamed('limit'), cursor: anyNamed('cursor')))
          .thenAnswer((_) async => result);
      when(mockApiService.getUnreadNotificationCount()).thenAnswer((_) async => 1);

      await notificationService.fetchNotifications();

      when(mockApiService.markAllNotificationsAsRead()).thenThrow(Exception('Mark all failed'));

      await notificationService.markAllAsRead();

      // Optimistic update
      expect(notificationService.unreadCount, 0);
    });
  });
}
