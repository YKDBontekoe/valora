import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/models/map_city_insight.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/screens/insights/insights_screen.dart';
import 'package:valora_app/services/api_service.dart';

class _FakeApiService extends ApiService {
  _FakeApiService(this._cities);

  final List<MapCityInsight> _cities;

  @override
  Future<List<MapCityInsight>> getCityInsights() async {
    return _cities;
  }
}

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  testWidgets('InsightsScreen renders upgraded map controls and legend', (
    tester,
  ) async {
    final api = _FakeApiService([
      MapCityInsight(
        city: 'Amsterdam',
        count: 125,
        location: const LatLng(52.3676, 4.9041),
        compositeScore: 78,
      ),
    ]);

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<InsightsProvider>(
          create: (_) => InsightsProvider(api),
          child: const InsightsScreen(),
        ),
      ),
    );

    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.text('Area Insights'), findsOneWidget);
    expect(find.byKey(const Key('insights_map_legend')), findsOneWidget);
    expect(find.byKey(const Key('insights_zoom_in_button')), findsOneWidget);
    expect(find.byKey(const Key('insights_zoom_out_button')), findsOneWidget);
  });
}
