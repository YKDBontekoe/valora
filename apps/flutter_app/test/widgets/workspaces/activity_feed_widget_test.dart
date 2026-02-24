import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/workspaces/activity_feed_widget.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/widgets/common/valora_empty_state.dart';

void main() {
  testWidgets('ActivityFeedWidget renders empty state', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: const ActivityFeedWidget(activities: []),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.byType(ValoraEmptyState), findsOneWidget);
    expect(find.text('No recent activity'), findsOneWidget);
  });

  testWidgets('ActivityFeedWidget renders activity logs correctly', (WidgetTester tester) async {
    final activities = [
      ActivityLog(
        id: '1',
        actorId: 'user1',
        type: 'CREATED_LISTING',
        summary: 'Created listing 123',
        createdAt: DateTime.now(),
      ),
      ActivityLog(
        id: '2',
        actorId: 'user2',
        type: 'COMMENTED',
        summary: 'Commented on listing 456',
        createdAt: DateTime.now().subtract(const Duration(hours: 2)),
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ActivityFeedWidget(activities: activities),
        ),
      ),
    );

    await tester.pump();
    await tester.pumpAndSettle();

    expect(find.text('Created listing 123'), findsOneWidget);
    expect(find.text('Commented on listing 456'), findsOneWidget);
    expect(find.text('user1'), findsOneWidget);
    expect(find.text('user2'), findsOneWidget);

    // Check for icons
    expect(find.byIcon(Icons.history_rounded), findsNWidgets(2));
  });
}
