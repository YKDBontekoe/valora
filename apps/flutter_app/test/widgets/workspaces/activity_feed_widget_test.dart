import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/workspaces/activity_feed_widget.dart';
import 'package:valora_app/models/activity_log.dart';

void main() {
  testWidgets('ActivityFeedWidget renders empty state', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: ActivityFeedWidget(activities: []),
        ),
      ),
    );

    expect(find.text('No recent activity.'), findsOneWidget);
  });

  testWidgets('ActivityFeedWidget renders activities', (WidgetTester tester) async {
    final activities = [
      ActivityLog(
        id: '1',
        actorId: 'User1',
        type: 'update',
        summary: 'Updated workspace',
        createdAt: DateTime(2023, 1, 1, 12, 0),
      ),
      ActivityLog(
        id: '2',
        actorId: 'User2',
        type: 'comment',
        summary: 'Added a comment',
        createdAt: DateTime(2023, 1, 2, 12, 0),
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ActivityFeedWidget(activities: activities),
        ),
      ),
    );

    expect(find.text('Updated workspace'), findsOneWidget);
    expect(find.text('Added a comment'), findsOneWidget);
    expect(find.byIcon(Icons.history), findsNWidgets(2));
  });
}
