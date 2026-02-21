import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/insights/map_query_input.dart';

void main() {
  testWidgets('MapQueryInput submits query on enter', (WidgetTester tester) async {
    String? submittedQuery;

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: MapQueryInput(
          onQuery: (q) => submittedQuery = q,
        ),
      ),
    ));

    final textField = find.byType(TextField);
    await tester.enterText(textField, 'Safe areas');
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await tester.pump();

    expect(submittedQuery, 'Safe areas');
  });

  testWidgets('MapQueryInput submits query on button press', (WidgetTester tester) async {
    String? submittedQuery;

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: MapQueryInput(
          onQuery: (q) => submittedQuery = q,
        ),
      ),
    ));

    final textField = find.byType(TextField);
    await tester.enterText(textField, 'Cheap areas');

    final button = find.byIcon(Icons.arrow_upward_rounded);
    await tester.tap(button);
    await tester.pump();

    expect(submittedQuery, 'Cheap areas');
  });

  testWidgets('MapQueryInput shows loading', (WidgetTester tester) async {
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: MapQueryInput(
          onQuery: (_) {},
          isLoading: true,
        ),
      ),
    ));

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    expect(find.byIcon(Icons.arrow_upward_rounded), findsNothing);
  });
}
