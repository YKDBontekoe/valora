import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/widgets/notifications/notification_sheet.dart';
import '../../mocks/mock_notification_service.dart';

void main() {
  late MockNotificationService mockService;

  setUp(() {
    mockService = MockNotificationService();
  });

  Widget createWidget() {
    return ChangeNotifierProvider<NotificationService>.value(
      value: mockService,
      child: const MaterialApp(
        home: Scaffold(
          body: NotificationSheet(),
        ),
      ),
    );
  }

  testWidgets('NotificationSheet shows loading indicator', (tester) async {
    when(mockService.isLoading).thenReturn(true);
    when(mockService.notifications).thenReturn([]);
    when(mockService.error).thenReturn(null);

    await tester.pumpWidget(createWidget());

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });

  testWidgets('NotificationSheet shows empty state', (tester) async {
    when(mockService.isLoading).thenReturn(false);
    when(mockService.notifications).thenReturn([]);
    when(mockService.error).thenReturn(null);

    await tester.pumpWidget(createWidget());

    expect(find.text('No notifications yet'), findsOneWidget);
  });

  testWidgets('NotificationSheet shows list of notifications', (tester) async {
    final notifications = [
      ValoraNotification(
        id: '1',
        title: 'Note 1',
        body: 'Body 1',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      ),
      ValoraNotification(
        id: '2',
        title: 'Note 2',
        body: 'Body 2',
        isRead: true,
        createdAt: DateTime.now(),
        type: NotificationType.system,
      ),
    ];

    when(mockService.isLoading).thenReturn(false);
    when(mockService.notifications).thenReturn(notifications);
    when(mockService.error).thenReturn(null);

    await tester.pumpWidget(createWidget());

    expect(find.text('Note 1'), findsOneWidget);
    expect(find.text('Note 2'), findsOneWidget);
    expect(find.text('Mark all as read'), findsOneWidget);
  });

  testWidgets('NotificationSheet calls markAllAsRead', (tester) async {
    final notifications = [
      ValoraNotification(
        id: '1',
        title: 'Note 1',
        body: 'Body 1',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      ),
    ];

    when(mockService.isLoading).thenReturn(false);
    when(mockService.notifications).thenReturn(notifications);
    when(mockService.error).thenReturn(null);

    await tester.pumpWidget(createWidget());

    await tester.tap(find.text('Mark all as read'));
    verify(mockService.markAllAsRead()).called(1);
  });
}
