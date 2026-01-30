import 'package:flutter_test/flutter_test.dart';

import 'package:valora_app/main.dart';

void main() {
  testWidgets('App renders home screen', (WidgetTester tester) async {
    await tester.pumpWidget(const ValoraApp());

    expect(find.text('Valora'), findsOneWidget);
  });
}
