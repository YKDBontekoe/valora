import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/screens/notifications_screen.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

@GenerateNiceMocks([MockSpec<NotificationService>()])
import 'notifications_screen_test.mocks.dart';

void main() {
  late MockNotificationService mockNotificationService;

  setUp(() {
    mockNotificationService = MockNotificationService();
    // Default behaviors
    when(mockNotificationService.notifications).thenReturn([]);
    when(mockNotificationService.isLoading).thenReturn(false);
    when(mockNotificationService.isLoadingMore).thenReturn(false);
    when(mockNotificationService.error).thenReturn(null);
    when(mockNotificationService.unreadCount).thenReturn(0);
    when(mockNotificationService.hasMore).thenReturn(false);
  });

  Widget createWidgetUnderTest() {
    return MaterialApp(
      home: ChangeNotifierProvider<NotificationService>.value(
        value: mockNotificationService,
        child: const NotificationsScreen(),
      ),
    );
  }

  testWidgets('NotificationsScreen shows loading indicator initially', (WidgetTester tester) async {
    when(mockNotificationService.isLoading).thenReturn(true);
    when(mockNotificationService.notifications).thenReturn([]);

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pump(); // Start frame

    expect(find.byType(ValoraLoadingIndicator), findsOneWidget);
    // Pump a few frames to allow animations to progress without waiting indefinitely
    await tester.pump(const Duration(milliseconds: 100));
  });

  testWidgets('NotificationsScreen shows empty state', (WidgetTester tester) async {
    when(mockNotificationService.isLoading).thenReturn(false);
    when(mockNotificationService.notifications).thenReturn([]);

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    expect(find.text('No notifications'), findsOneWidget);
  });

  testWidgets('NotificationsScreen shows error state', (WidgetTester tester) async {
    when(mockNotificationService.isLoading).thenReturn(false);
    when(mockNotificationService.notifications).thenReturn([]);
    when(mockNotificationService.error).thenReturn('Network error');

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    expect(find.text('Something went wrong'), findsOneWidget);
    // Should show generic message, not the raw error
    expect(find.text('Network error'), findsNothing);
    expect(find.textContaining('We couldn\'t load your notifications'), findsOneWidget);
  });

  testWidgets('NotificationsScreen displays list of notifications', (WidgetTester tester) async {
    final notifications = [
      ValoraNotification(
        id: '1',
        title: 'Test Title 1',
        body: 'Test Body 1',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
      ),
      ValoraNotification(
        id: '2',
        title: 'Test Title 2',
        body: 'Test Body 2',
        isRead: true,
        createdAt: DateTime.now().subtract(const Duration(days: 1)),
        type: NotificationType.priceDrop,
      ),
    ];

    when(mockNotificationService.notifications).thenReturn(notifications);
    when(mockNotificationService.unreadCount).thenReturn(1);

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    expect(find.text('Test Title 1'), findsOneWidget);
    expect(find.text('Test Body 1'), findsOneWidget);
    expect(find.text('Test Title 2'), findsOneWidget);

    // Check for "Mark all read" button
    expect(find.text('Mark all read'), findsOneWidget);
  });

  testWidgets('NotificationsScreen calls fetchNotifications on init', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pump();
    verify(mockNotificationService.fetchNotifications()).called(1);
    await tester.pumpAndSettle();
  });

  testWidgets('NotificationsScreen calls markAllAsRead', (WidgetTester tester) async {
    when(mockNotificationService.notifications).thenReturn([
       ValoraNotification(id: '1', title: 'A', body: 'B', isRead: false, createdAt: DateTime.now(), type: NotificationType.info)
    ]);
    when(mockNotificationService.unreadCount).thenReturn(1);

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Mark all read'));
    verify(mockNotificationService.markAllAsRead()).called(1);
  });

  testWidgets('NotificationsScreen calls markAsRead on tap', (WidgetTester tester) async {
    final notification = ValoraNotification(
        id: '1',
        title: 'Title',
        body: 'Body',
        isRead: false,
        createdAt: DateTime.now(),
        type: NotificationType.info,
    );
    when(mockNotificationService.notifications).thenReturn([notification]);

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Title'));
    verify(mockNotificationService.markAsRead('1')).called(1);
  });

  testWidgets('NotificationsScreen swipes to delete', (WidgetTester tester) async {
    final notification = ValoraNotification(
        id: '1',
        title: 'Title',
        body: 'Body',
        isRead: true,
        createdAt: DateTime.now(),
        type: NotificationType.info,
    );
    when(mockNotificationService.notifications).thenReturn([notification]);

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.drag(find.text('Title'), const Offset(-500, 0));
    await tester.pumpAndSettle();

    verify(mockNotificationService.deleteNotification('1')).called(1);
    expect(find.text('Notification deleted'), findsOneWidget);
  });
}
