import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/widgets/workspaces/activity_feed_widget.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

void main() {
  Widget createWidgetUnderTest(List<ActivityLog> activities) {
    return MaterialApp(
      home: Scaffold(
        body: ActivityFeedWidget(activities: activities),
      ),
    );
  }

  testWidgets('renders empty state when activities list is empty', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest([]));
    await tester.pumpAndSettle();

    expect(find.byType(ValoraEmptyState), findsOneWidget);
    expect(find.text('No recent activity'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('renders list of activities', (tester) async {
    final List<ActivityLog> activities = [
      ActivityLog(id: '1', type: 'listing_saved', actorId: 'User 1', summary: 'Added a listing', createdAt: DateTime(2023, 1, 1, 10, 0)),
      ActivityLog(id: '2', type: 'comment_added', actorId: 'User 2', summary: 'Left a comment', createdAt: DateTime(2023, 1, 1, 12, 0)),
    ];

    await tester.pumpWidget(createWidgetUnderTest(activities));
    await tester.pumpAndSettle();

    expect(find.byType(ValoraCard), findsNWidgets(2));
    expect(find.text('Added a listing'), findsOneWidget);
    expect(find.text('Left a comment'), findsOneWidget);
    expect(find.textContaining('User 1'), findsOneWidget);
    expect(find.textContaining('User 2'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });
}
