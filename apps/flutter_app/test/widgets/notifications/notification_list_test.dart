import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/widgets/notifications/notification_list.dart';
import 'package:valora_app/widgets/valora_widgets.dart';
import '../../mocks/mock_notification_service.dart';

void main() {
  late MockNotificationService mockService;

  setUp(() {
    mockService = MockNotificationService();
  });

  Widget createWidget({bool asSliver = false}) {
    return MaterialApp(
      home: ChangeNotifierProvider<NotificationService>.value(
        value: mockService,
        child: Scaffold(
          body: asSliver
              ? CustomScrollView(
                  slivers: [
                    const NotificationList(asSliver: true),
                  ],
                )
              : const NotificationList(),
        ),
      ),
    );
  }

  testWidgets('shows loading indicator when loading', (WidgetTester tester) async {
    mockService.setIsLoading(true);
    await tester.pumpWidget(createWidget());
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.byType(ValoraLoadingIndicator), findsOneWidget);
    expect(find.text('Loading notifications...'), findsOneWidget);
  });

  testWidgets('shows empty state when no notifications', (WidgetTester tester) async {
    mockService.setIsLoading(false);
    mockService.setNotifications([]);
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle();

    expect(find.text('No notifications'), findsOneWidget);
    expect(find.byType(ValoraEmptyState), findsOneWidget);
  });

  testWidgets('shows notifications list', (WidgetTester tester) async {
    final notifications = [
      ValoraNotification(
        id: '1',
        title: 'Title 1',
        body: 'Body 1',
        type: NotificationType.info,
        createdAt: DateTime.now(),
        isRead: false,
      ),
      ValoraNotification(
        id: '2',
        title: 'Title 2',
        body: 'Body 2',
        type: NotificationType.info,
        createdAt: DateTime.now(),
        isRead: true,
      ),
    ];
    mockService.setNotifications(notifications);
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle();

    expect(find.text('Title 1'), findsOneWidget);
    expect(find.text('Title 2'), findsOneWidget);
  });
}
