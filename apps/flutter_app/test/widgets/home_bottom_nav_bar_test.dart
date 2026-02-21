import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/home/home_bottom_nav_bar.dart';

void main() {
  testWidgets('HomeBottomNavBar renders and handles taps', (
    WidgetTester tester,
  ) async {
    int selectedIndex = 0;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: StatefulBuilder(
            builder: (context, setState) {
              return Stack(
                children: [
                  HomeBottomNavBar(
                    currentIndex: selectedIndex,
                    onTap: (index) {
                      setState(() {
                        selectedIndex = index;
                      });
                    },
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Verify initial state
    expect(find.byIcon(Icons.search_rounded), findsOneWidget);
    expect(find.byIcon(Icons.map_rounded), findsOneWidget); // Insights
    expect(find.byIcon(Icons.settings_rounded), findsOneWidget);

    expect(find.byTooltip('Search'), findsOneWidget);
    expect(find.byTooltip('Insights'), findsOneWidget);
    expect(find.byTooltip('Settings'), findsOneWidget);

    // Tap Insights (index 1)
    await tester.tap(find.byTooltip('Insights'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 1);

    // Tap Settings (index 2)
    await tester.tap(find.byTooltip('Settings'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 2);

    // Tap Search (index 0)
    await tester.tap(find.byTooltip('Search'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 0);
  });

  testWidgets('HomeBottomNavBar hides selected labels in compact layouts', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      MediaQuery(
        data: const MediaQueryData(size: Size(360, 800)),
        child: MaterialApp(
          home: Scaffold(
            bottomNavigationBar: HomeBottomNavBar(
              currentIndex: 2,
              onTap: (_) {},
            ),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Settings'), findsNothing);
    expect(find.byTooltip('Settings'), findsOneWidget);
  });
}
