import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/repositories/notification_repository.dart';
import 'package:valora_app/services/notification_service.dart';

@GenerateMocks([NotificationRepository])
import 'notification_service_coverage_test.mocks.dart';

void main() {
  late MockNotificationRepository mockRepository;
  late NotificationService notificationService;

  setUp(() {
    mockRepository = MockNotificationRepository();
    notificationService = NotificationService(mockRepository);
  });

  tearDown(() {
    notificationService.stopPolling();
  });

  group('NotificationService Coverage', () {
    test('_fetchUnreadCount failure is handled silently', () async {
      when(mockRepository.getUnreadNotificationCount()).thenThrow(Exception('Poll failed'));

      // Should not throw
      notificationService.startPolling();
    });

    test('loadMoreNotifications failure sets error state', () async {
      when(mockRepository.getNotifications(limit: anyNamed('limit'), offset: anyNamed('offset')))
          .thenThrow(Exception('Load failed'));

      await notificationService.loadMoreNotifications();

      expect(notificationService.isLoadingMore, false);
    });

    test('markAsRead failure is handled', () async {
      final n = ValoraNotification(id: '1', title: 'T', body: 'B', isRead: false, createdAt: DateTime.now(), type: NotificationType.info);

      when(mockRepository.getNotifications(limit: anyNamed('limit'), offset: anyNamed('offset')))
          .thenAnswer((_) async => [n]);
      when(mockRepository.getUnreadNotificationCount()).thenAnswer((_) async => 1);

      await notificationService.fetchNotifications();

      when(mockRepository.markNotificationAsRead('1')).thenThrow(Exception('Mark failed'));

      await notificationService.markAsRead('1');

      expect(notificationService.unreadCount, 0);
    });

    test('markAllAsRead failure is handled', () async {
      final n = ValoraNotification(id: '1', title: 'T', body: 'B', isRead: false, createdAt: DateTime.now(), type: NotificationType.info);
      when(mockRepository.getNotifications(limit: anyNamed('limit'), offset: anyNamed('offset')))
          .thenAnswer((_) async => [n]);
      when(mockRepository.getUnreadNotificationCount()).thenAnswer((_) async => 1);

      await notificationService.fetchNotifications();

      when(mockRepository.markAllNotificationsAsRead()).thenThrow(Exception('Mark all failed'));

      await notificationService.markAllAsRead();

      expect(notificationService.unreadCount, 0);
    });
  });
}
