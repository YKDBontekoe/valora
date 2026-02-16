import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:mockito/mockito.dart';

class MockApiService extends Mock implements ApiService {}
class MockPdokService extends Mock implements PdokService {}

void main() {
  testWidgets('ContextReportScreen renders search form components', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(MaterialApp(
        home: Provider<ApiService>.value(
          value: MockApiService(),
          child: ContextReportScreen(pdokService: MockPdokService()),
        ),
      ));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.text('Property Analytics'), findsAtLeastNWidgets(1));
      expect(find.text('Search Property'), findsOneWidget);
    });
  });
}
