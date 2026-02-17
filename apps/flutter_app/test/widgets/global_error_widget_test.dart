import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/global_error_widget.dart';

void main() {
  testWidgets('GlobalErrorWidget renders correctly', (WidgetTester tester) async {
    final details = FlutterErrorDetails(
      exception: Exception('Test Exception'),
      stack: StackTrace.fromString('Test Stack Trace'),
    );

    await tester.pumpWidget(MaterialApp(
      home: GlobalErrorWidget(details: details),
    ));

    expect(find.text("We're sorry, something went wrong"), findsOneWidget);
    expect(find.text('Restart'), findsOneWidget);
    if (kDebugMode) {
      expect(find.text('Copy Error'), findsOneWidget);
      expect(find.textContaining('Test Exception'), findsOneWidget);
    }
  });

  testWidgets('GlobalErrorWidget copy error button works', (WidgetTester tester) async {
    // Only verify in debug mode as the button is only shown there
    if (!kDebugMode) return;

    final details = FlutterErrorDetails(
      exception: Exception('Test Exception'),
      stack: StackTrace.fromString('Test Stack Trace'),
    );

    // Mock clipboard
    final log = <MethodCall>[];
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(SystemChannels.platform, (MethodCall methodCall) async {
      log.add(methodCall);
      return null;
    });

    await tester.pumpWidget(MaterialApp(
      home: GlobalErrorWidget(details: details),
    ));

    await tester.tap(find.text('Copy Error'));
    await tester.pump();

    expect(log, isNotEmpty);
    expect(log.last.method, 'Clipboard.setData');
    expect(log.last.arguments['text'], contains('Test Exception'));
  });
}
