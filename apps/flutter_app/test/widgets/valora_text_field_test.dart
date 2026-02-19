import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/common/valora_text_field.dart';
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

  Widget createWidget({
    FocusNode? focusNode,
    String? label,
  }) {
    return MaterialApp(
      theme: ThemeData(
        fontFamily: 'Roboto',
        useMaterial3: true,
      ),
      home: Scaffold(
        body: ValoraTextField(
          focusNode: focusNode,
          label: label,
        ),
      ),
    );
  }

  testWidgets('ValoraTextField manages internal focus node when none provided', (tester) async {
    await tester.pumpWidget(createWidget(label: 'Test Input'));

    // Find the internal TextField
    final textField = find.byType(TextField);
    expect(textField, findsOneWidget);

    // Initial state: not focused
    // Removed unused variable 'state'
    // We can't access private state easily, but we can check if the label color changes or shadow appears
    // Just verifying it renders without error first
    expect(find.text('Test Input'), findsOneWidget);

    // Tap to focus
    await tester.tap(textField);
    await tester.pumpAndSettle();

    // Should have focus
    final focusNode = (textField.evaluate().first.widget as TextField).focusNode;
    expect(focusNode?.hasFocus, isTrue);
  });

  testWidgets('ValoraTextField respects external focus node', (tester) async {
    final focusNode = FocusNode();
    await tester.pumpWidget(createWidget(focusNode: focusNode, label: 'External Node'));

    final textField = find.byType(TextField);
    expect(textField, findsOneWidget);

    // Request focus externally
    focusNode.requestFocus();
    await tester.pumpAndSettle();

    // Widget should reflect focus state
    final internalField = textField.evaluate().first.widget as TextField;
    expect(internalField.focusNode, equals(focusNode));
    expect(focusNode.hasFocus, isTrue);
  });

  testWidgets('ValoraTextField updates focus listener when FocusNode changes', (tester) async {
    final focusNode1 = FocusNode();
    final focusNode2 = FocusNode();

    // 1. Pump with first node
    await tester.pumpWidget(createWidget(focusNode: focusNode1));
    focusNode1.requestFocus();
    await tester.pumpAndSettle();
    expect(focusNode1.hasFocus, isTrue);

    // 2. Rebuild with second node
    await tester.pumpWidget(createWidget(focusNode: focusNode2));

    // The widget should have detached from node1 and attached to node2
    // Focus node 2 is not focused yet
    expect(focusNode2.hasFocus, isFalse);

    // Focus node 2
    focusNode2.requestFocus();
    await tester.pumpAndSettle();
    expect(focusNode2.hasFocus, isTrue);

    // Changing focus on node 1 should effectively do nothing to the widget now,
    // but verifying "no crash" is the main thing here.
  });
}
