import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:valora_app/main.dart';

void main() {
  testWidgets('App renders home screen', (WidgetTester tester) async {
    // Save the original builder
    final originalBuilder = ErrorWidget.builder;

    try {
      await tester.pumpWidget(const ValoraApp());
      expect(find.text('Valora'), findsOneWidget);
    } finally {
      // Restore the original builder to avoid polluting other tests
      ErrorWidget.builder = originalBuilder;
    }
  });
}
