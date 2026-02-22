import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/common/valora_settings_tile.dart';

void main() {
  testWidgets('ValoraSettingsTile renders title and subtitle', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: ValoraSettingsTile(
            icon: Icons.settings,
            title: 'Test Title',
            subtitle: 'Test Subtitle',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Test Title'), findsOneWidget);
    expect(find.text('Test Subtitle'), findsOneWidget);
    expect(find.byIcon(Icons.settings), findsOneWidget);
  });

  testWidgets('ValoraSettingsTile handles onTap', (WidgetTester tester) async {
    bool tapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ValoraSettingsTile(
            icon: Icons.settings,
            title: 'Test Title',
            onTap: () => tapped = true,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byType(ValoraSettingsTile));
    await tester.pumpAndSettle();

    expect(tapped, isTrue);
  });

  testWidgets('ValoraSettingsTile is disabled when onTap is null', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: ValoraSettingsTile(
            icon: Icons.settings,
            title: 'Test Title',
            onTap: null,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final mouseRegion = tester.widget<MouseRegion>(
      find
          .descendant(
            of: find.byType(ValoraSettingsTile),
            matching: find.byType(MouseRegion),
          )
          .first,
    );

    expect(mouseRegion.cursor, SystemMouseCursors.basic);
    expect(mouseRegion.onEnter, isNull);
    expect(mouseRegion.onExit, isNull);
  });

  testWidgets('ValoraSettingsTile shows click cursor when enabled', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ValoraSettingsTile(
            icon: Icons.settings,
            title: 'Test Title',
            onTap: () {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final mouseRegion = tester.widget<MouseRegion>(
      find
          .descendant(
            of: find.byType(ValoraSettingsTile),
            matching: find.byType(MouseRegion),
          )
          .first,
    );

    expect(mouseRegion.cursor, SystemMouseCursors.click);
    expect(mouseRegion.onEnter, isNotNull);
    expect(mouseRegion.onExit, isNotNull);
  });
}
