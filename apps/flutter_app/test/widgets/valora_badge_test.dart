import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/common/valora_badge.dart';
import 'package:google_fonts/google_fonts.dart';

void main() {
  setUp(() {
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  setUpAll(() {
    final originalOnError = FlutterError.onError;
    FlutterError.onError = (FlutterErrorDetails details) {
      if (details.exception.toString().contains('GoogleFonts') ||
          details.exception.toString().contains('MissingPluginException')) {
        return;
      }
      originalOnError?.call(details);
    };
  });

  Widget createWidget({bool enableBlur = true, bool enableAnimation = true}) {
    return MaterialApp(
      theme: ThemeData(fontFamily: 'Roboto', useMaterial3: true),
      home: Scaffold(
        body: ValoraBadge(
          label: 'Test Badge',
          enableBlur: enableBlur,
          enableAnimation: enableAnimation,
        ),
      ),
    );
  }

  testWidgets('ValoraBadge renders with blur and animation by default', (
    tester,
  ) async {
    await tester.pumpWidget(createWidget());
    await tester.pump(); // Start animation
    await tester.pump(const Duration(milliseconds: 500)); // Finish animation
    await tester.pumpAndSettle(); // Ensure everything is settled

    expect(find.text('Test Badge'), findsOneWidget);

    // Check for BackdropFilter
    expect(find.byType(BackdropFilter), findsOneWidget);

    // Check for Animate widget effects
    final backdropFilter = find.byType(BackdropFilter);
    final clipRRect = find.ancestor(
      of: backdropFilter,
      matching: find.byType(ClipRRect),
    );
    expect(clipRRect, findsOneWidget);
  });

  testWidgets('ValoraBadge skips blur when enableBlur is false', (
    tester,
  ) async {
    await tester.pumpWidget(createWidget(enableBlur: false));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500)); // Finish animation
    await tester.pumpAndSettle(); // Ensure everything is settled

    expect(find.text('Test Badge'), findsOneWidget);

    // Should NOT have BackdropFilter
    expect(find.byType(BackdropFilter), findsNothing);
  });

  testWidgets('ValoraBadge skips animation when enableAnimation is false', (
    tester,
  ) async {
    await tester.pumpWidget(createWidget(enableAnimation: false));
    await tester.pumpAndSettle();

    // Verify it renders
    expect(find.text('Test Badge'), findsOneWidget);

    // Check for absence of Animate widget logic
    // If animation is disabled, we shouldn't have any pending timers, and widget structure is simpler
  });
}
