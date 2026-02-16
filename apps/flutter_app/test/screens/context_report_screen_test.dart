import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:mockito/mockito.dart';

class MockApiService extends Mock implements ApiService {}

void main() {
  testWidgets('ContextReportScreen renders search form initially', (tester) async {
    await tester.pumpWidget(MaterialApp(
      home: Provider<ApiService>.value(
        value: MockApiService(),
        child: const ContextReportScreen(),
      ),
    ));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('Property Analytics'), findsAtLeastNWidgets(1));
    expect(find.text('Search Property'), findsOneWidget);
  });
}
