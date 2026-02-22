import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/home/home_bottom_nav_bar.dart';

class _MockApiService extends ApiService {
  _MockApiService() : super();
}

Widget _buildTestWidget({
  required int currentIndex,
  required ValueChanged<int> onTap,
  Size? screenSize,
}) {
  final apiService = _MockApiService();
  final notificationService = NotificationService(apiService);

  Widget child = MaterialApp(
    home: Scaffold(
      body: StatefulBuilder(
        builder: (context, setState) {
          return Stack(
            children: [
              HomeBottomNavBar(currentIndex: currentIndex, onTap: onTap),
            ],
          );
        },
      ),
    ),
  );

  if (screenSize != null) {
    child = MediaQuery(
      data: MediaQueryData(size: screenSize),
      child: child,
    );
  }

  return ChangeNotifierProvider<NotificationService>.value(
    value: notificationService,
    child: child,
  );
}

void main() {
  testWidgets('HomeBottomNavBar renders and handles taps', (
    WidgetTester tester,
  ) async {
    int selectedIndex = 0;

    await tester.pumpWidget(
      StatefulBuilder(
        builder: (context, setState) {
          return _buildTestWidget(
            currentIndex: selectedIndex,
            onTap: (index) {
              setState(() {
                selectedIndex = index;
              });
            },
          );
        },
      ),
    );

    await tester.pumpAndSettle();

    // Verify 4 nav items exist
    expect(find.byTooltip('Search'), findsOneWidget);
    expect(find.byTooltip('Insights'), findsOneWidget);
    expect(find.byTooltip('Alerts'), findsOneWidget);
    expect(find.byTooltip('Settings'), findsOneWidget);

    // Tap Insights (index 1)
    await tester.tap(find.byTooltip('Insights'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 1);

    // Tap Alerts (index 2)
    await tester.tap(find.byTooltip('Alerts'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 2);

    // Tap Settings (index 3)
    await tester.tap(find.byTooltip('Settings'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 3);

    // Tap Search (index 0)
    await tester.tap(find.byTooltip('Search'));
    await tester.pumpAndSettle();
    expect(selectedIndex, 0);
  });

  testWidgets('HomeBottomNavBar hides selected labels in compact layouts', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      _buildTestWidget(
        currentIndex: 2,
        onTap: (_) {},
        screenSize: const Size(360, 800),
      ),
    );

    await tester.pumpAndSettle();

    // In compact mode the selected label text should be hidden
    expect(find.text('Alerts'), findsNothing);
    expect(find.byTooltip('Alerts'), findsOneWidget);
  });
}
