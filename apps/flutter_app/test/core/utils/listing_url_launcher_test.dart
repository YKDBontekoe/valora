import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:plugin_platform_interface/plugin_platform_interface.dart';
import 'package:url_launcher_platform_interface/url_launcher_platform_interface.dart';
import 'package:valora_app/core/utils/listing_url_launcher.dart';

class MockUrlLauncher extends Mock with MockPlatformInterfaceMixin implements UrlLauncherPlatform {
  bool launchResult = false;
  bool throwOnLaunch = false;

  @override
  Future<bool> canLaunch(String url) async => true;

  @override
  Future<bool> launch(
    String url, {
    bool useSafariVC = false,
    bool useWebView = false,
    bool enableJavaScript = false,
    bool enableDomStorage = false,
    bool universalLinksOnly = false,
    Map<String, String> headers = const <String, String>{},
    String? webOnlyWindowName,
  }) async {
    if (throwOnLaunch) {
      throw Exception('Launch failed');
    }
    return launchResult;
  }
}

void main() {
  late MockUrlLauncher mockLauncher;

  setUp(() {
    mockLauncher = MockUrlLauncher();
    UrlLauncherPlatform.instance = mockLauncher;
  });

  testWidgets('openExternalLink shows error snackbar on failure', (WidgetTester tester) async {
    mockLauncher.launchResult = false;

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (context) => ElevatedButton(
            onPressed: () => ListingUrlLauncher.openExternalLink(context, 'https://example.com'),
            child: const Text('Open'),
          ),
        ),
      ),
    ));

    await tester.tap(find.text('Open'));
    await tester.pump();

    expect(find.text('Could not open link'), findsOneWidget);
  });

  testWidgets('openExternalLink shows error snackbar on exception', (WidgetTester tester) async {
    mockLauncher.throwOnLaunch = true;

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (context) => ElevatedButton(
            onPressed: () => ListingUrlLauncher.openExternalLink(context, 'https://example.com'),
            child: const Text('Open'),
          ),
        ),
      ),
    ));

    await tester.tap(find.text('Open'));
    await tester.pump();

    expect(find.text('Could not open link'), findsOneWidget);
  });

  testWidgets('contactBroker shows error snackbar on exception', (WidgetTester tester) async {
    mockLauncher.throwOnLaunch = true;

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: Builder(
          builder: (context) => ElevatedButton(
            onPressed: () => ListingUrlLauncher.contactBroker(context, '12345'),
            child: const Text('Call'),
          ),
        ),
      ),
    ));

    await tester.tap(find.text('Call'));
    await tester.pump();

    expect(find.text('Could not launch dialer'), findsOneWidget);
  });
}
