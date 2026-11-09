import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/report/next_actions_widget.dart';
import 'package:valora_app/models/report_action.dart';

void main() {
  testWidgets('NextActionsWidget renders actions', (tester) async {
    final actions = [
      const ReportAction(
        id: '1',
        title: 'Action 1',
        description: 'Desc 1',
        icon: Icons.add,
        type: ActionType.save,
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: NextActionsWidget(
            actions: actions,
            onAction: (_) {},
            onDismiss: (_) {},
          ),
        ),
      ),
    );

    // Pump animation
    await tester.pumpAndSettle();

    expect(find.text('Recommended Actions'), findsOneWidget);
    expect(find.text('Action 1'), findsOneWidget);
    expect(find.text('Desc 1'), findsOneWidget);
  });

  testWidgets('NextActionsWidget handles tap and dismiss', (tester) async {
    final actions = [
      const ReportAction(
        id: '1',
        title: 'Action 1',
        description: 'Desc 1',
        icon: Icons.add,
        type: ActionType.save,
      ),
    ];

    ReportAction? tappedAction;
    ReportAction? dismissedAction;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: NextActionsWidget(
            actions: actions,
            onAction: (a) => tappedAction = a,
            onDismiss: (a) => dismissedAction = a,
          ),
        ),
      ),
    );

    // Pump animation
    await tester.pumpAndSettle();

    await tester.tap(find.text('Action 1'));
    expect(tappedAction, actions[0]);

    // Dismiss icon is close_rounded
    await tester.tap(find.byIcon(Icons.close_rounded));
    expect(dismissedAction, actions[0]);
  });
}
