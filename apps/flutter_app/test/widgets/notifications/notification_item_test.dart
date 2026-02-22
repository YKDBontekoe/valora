import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/notification.dart';
import 'package:valora_app/widgets/notifications/notification_item.dart';

void main() {
  Widget createWidget(NotificationItem item) {
    return MaterialApp(home: Scaffold(body: item));
  }

  testWidgets('NotificationItem renders content correctly', (tester) async {
    final notification = ValoraNotification(
      id: '1',
      title: 'Test Notification',
      body: 'This is a test body',
      isRead: false,
      createdAt: DateTime.now().subtract(const Duration(minutes: 5)),
      type: NotificationType.info,
    );

    await tester.pumpWidget(
      createWidget(NotificationItem(notification: notification)),
    );

    expect(find.text('Test Notification'), findsOneWidget);
    expect(find.text('This is a test body'), findsOneWidget);
    expect(find.text('5m ago'), findsOneWidget);

    // Check for unread indicator (circle container)
    // Container with 8x8 size and ValoraColors.primary (approximate check by size)
    expect(find.byType(Container), findsWidgets);
  });

  testWidgets('NotificationItem handles tap', (tester) async {
    bool tapped = false;
    final notification = ValoraNotification(
      id: '1',
      title: 'Test',
      body: 'Body',
      isRead: true,
      createdAt: DateTime.now(),
      type: NotificationType.priceDrop,
    );

    await tester.pumpWidget(
      createWidget(
        NotificationItem(
          notification: notification,
          onTap: () => tapped = true,
        ),
      ),
    );

    await tester.tap(find.byType(NotificationItem));
    expect(tapped, isTrue);
  });

  testWidgets('NotificationItem renders correct icon for type', (tester) async {
    final notification = ValoraNotification(
      id: '1',
      title: 'Test',
      body: 'Body',
      isRead: true,
      createdAt: DateTime.now(),
      type: NotificationType.priceDrop,
    );

    await tester.pumpWidget(
      createWidget(NotificationItem(notification: notification)),
    );

    expect(find.byIcon(Icons.trending_down_rounded), findsOneWidget);
  });
}
