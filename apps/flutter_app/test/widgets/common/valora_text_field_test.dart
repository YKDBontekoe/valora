import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/common/valora_text_field.dart';

void main() {
  testWidgets('ValoraTextField renders label and hint', (WidgetTester tester) async {
    await tester.pumpWidget(const MaterialApp(
      home: Scaffold(
        body: ValoraTextField(
          label: 'Test Label',
          hint: 'Test Hint',
        ),
      ),
    ));

    expect(find.text('Test Label'), findsOneWidget);
    expect(find.text('Test Hint'), findsOneWidget);
  });

  testWidgets('ValoraTextField renders prefix icon', (WidgetTester tester) async {
    await tester.pumpWidget(const MaterialApp(
      home: Scaffold(
        body: ValoraTextField(
          label: 'Test Label',
          prefixIcon: Icons.search,
        ),
      ),
    ));

    expect(find.byIcon(Icons.search), findsOneWidget);
  });

  testWidgets('ValoraTextField renders suffix icon', (WidgetTester tester) async {
    await tester.pumpWidget(const MaterialApp(
      home: Scaffold(
        body: ValoraTextField(
          label: 'Test Label',
          suffixIcon: Icon(Icons.clear),
        ),
      ),
    ));

    expect(find.byIcon(Icons.clear), findsOneWidget);
  });
}
