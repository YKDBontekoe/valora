import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/widgets/search/sort_options_sheet.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';

class FakeApiService extends Fake implements ApiService {}

void main() {
  testWidgets('SortOptionsSheet displays options with icons', (WidgetTester tester) async {
    final provider = SearchListingsProvider(apiService: FakeApiService());

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: SortOptionsSheet(
          provider: provider,
          onClose: () {},
        ),
      ),
    ));

    expect(find.text('Newest'), findsOneWidget);
    expect(find.text('Price: Low to High'), findsOneWidget);

    // Check for some icons
    expect(find.byIcon(Icons.calendar_today_rounded), findsOneWidget);
    expect(find.byIcon(Icons.trending_up_rounded), findsOneWidget);
    expect(find.byIcon(Icons.analytics_rounded), findsOneWidget);
  });
}
