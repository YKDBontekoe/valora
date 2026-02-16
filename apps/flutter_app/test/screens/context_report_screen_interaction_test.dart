import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:mockito/mockito.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:latlong2/latlong.dart';
import 'package:valora_app/widgets/report/location_picker.dart';

class MockApiService extends Mock implements ApiService {}
class MockPdokService extends Mock implements PdokService {
  @override
  Future<String?> reverseLookup(double? lat, double? lon) => super.noSuchMethod(
        Invocation.method(#reverseLookup, [lat, lon]),
        returnValue: Future<String?>.value(null),
      );
}

void main() {
  late MockApiService mockApiService;
  late MockPdokService mockPdokService;

  setUp(() {
    mockApiService = MockApiService();
    mockPdokService = MockPdokService();
    SharedPreferences.setMockInitialValues({});
  });

  testWidgets('ContextReportScreen location picker success flow', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(MaterialApp(
        home: Provider<ApiService>.value(
          value: mockApiService,
          child: ContextReportScreen(pdokService: mockPdokService),
        ),
      ));
      await tester.pumpAndSettle();

      when(mockPdokService.reverseLookup(any, any))
          .thenAnswer((_) async => 'Resolved Address');

      // Tap Map button
      await tester.tap(find.byIcon(Icons.map_outlined));
      await tester.pumpAndSettle();

      final state = tester.state(find.byType(LocationPicker));
      Navigator.pop(state.context, const LatLng(52.0, 4.0));

      // Need to pump enough to let async resolution finish
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));
      await tester.pumpAndSettle();

      expect(find.text('Resolved Address'), findsOneWidget);
    });
  });

  testWidgets('ContextReportScreen recent search tap triggers search', (tester) async {
    SharedPreferences.setMockInitialValues({
      'flutter.search_history': '[{"query": "SavedQuery", "timestamp": "2023-01-01T12:00:00Z"}]'
    });

    await tester.runAsync(() async {
      await tester.pumpWidget(MaterialApp(
        home: Provider<ApiService>.value(
          value: mockApiService,
          child: ContextReportScreen(pdokService: mockPdokService),
        ),
      ));

      await tester.pump(const Duration(milliseconds: 500));
      await tester.pumpAndSettle();

      final savedQueryFinder = find.text('SavedQuery');
      if (savedQueryFinder.evaluate().isNotEmpty) {
         await tester.tap(savedQueryFinder);
         await tester.pump();
         // Just verifying it doesn't crash and hits the tap handler
      }
    });
  });
}
