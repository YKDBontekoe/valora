import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:plugin_platform_interface/plugin_platform_interface.dart';
import 'package:url_launcher_platform_interface/url_launcher_platform_interface.dart';
import 'package:valora_app/core/utils/listing_url_launcher.dart';

// Mock UrlLauncherPlatform
class MockUrlLauncher extends Fake
    with MockPlatformInterfaceMixin
    implements UrlLauncherPlatform {
  @override
  Future<bool> launchUrl(String url, LaunchOptions options) async {
    if (url.contains('fail')) {
      return false;
    }
    if (url.contains('error')) {
      throw PlatformException(code: 'ERROR', message: 'Launch failed');
    }
    // Simulate failure for specific phone number
    if (url == 'tel:000') {
        throw PlatformException(code: 'ERROR', message: 'Launch failed');
    }
    if (url == 'tel:999') {
        return false;
    }
    return true;
  }

  @override
  Future<bool> canLaunch(String url) async => true;
}

void main() {
  setUp(() {
    UrlLauncherPlatform.instance = MockUrlLauncher();
  });

  Widget createWidgetUnderTest(Widget child) {
    return MaterialApp(
      home: Scaffold(
        body: child,
      ),
    );
  }

  group('ListingUrlLauncher', () {
    testWidgets('openExternalLink success', (tester) async {
      await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.openExternalLink(context, 'https://example.com'),
            child: const Text('Open'),
          );
        }),
      ));

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      expect(find.byType(SnackBar), findsNothing);
    });

    testWidgets('openExternalLink handles null url', (tester) async {
      await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.openExternalLink(context, null),
            child: const Text('Open'),
          );
        }),
      ));

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      expect(find.byType(SnackBar), findsNothing);
    });

    testWidgets('openExternalLink handles failure (launch returns false)', (tester) async {
      await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.openExternalLink(context, 'https://fail.com'),
            child: const Text('Open'),
          );
        }),
      ));

      await tester.tap(find.text('Open'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.byType(SnackBar), findsOneWidget);
      expect(find.text('Could not open link'), findsOneWidget);
    });

    testWidgets('openExternalLink handles exception', (tester) async {
      await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.openExternalLink(context, 'https://error.com'),
            child: const Text('Open'),
          );
        }),
      ));

      await tester.tap(find.text('Open'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.byType(SnackBar), findsOneWidget);
      expect(find.text('Could not open link'), findsOneWidget);
    });

    testWidgets('openExternalLink handles malformed URL', (tester) async {
       await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.openExternalLink(context, ':::'),
            child: const Text('Open'),
          );
        }),
      ));

      await tester.tap(find.text('Open'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.byType(SnackBar), findsOneWidget);
      expect(find.text('Could not open link'), findsOneWidget);
    });

    testWidgets('openMap uses coordinates when available', (tester) async {
       await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.openMap(context, 52.0, 4.0, 'Address', 'City'),
            child: const Text('Open'),
          );
        }),
      ));

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      expect(find.byType(SnackBar), findsNothing);
    });

    testWidgets('openMap uses address when coordinates missing', (tester) async {
       await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.openMap(context, null, null, 'Address', 'City'),
            child: const Text('Open'),
          );
        }),
      ));

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      expect(find.byType(SnackBar), findsNothing);
    });

    testWidgets('contactBroker success', (tester) async {
       await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.contactBroker(context, '+123456789'),
            child: const Text('Call'),
          );
        }),
      ));

      await tester.tap(find.text('Call'));
      await tester.pumpAndSettle();

      expect(find.byType(SnackBar), findsNothing);
    });

    testWidgets('contactBroker handles null phone', (tester) async {
       await tester.pumpWidget(createWidgetUnderTest(
        Builder(builder: (context) {
          return ElevatedButton(
            onPressed: () => ListingUrlLauncher.contactBroker(context, null),
            child: const Text('Call'),
          );
        }),
      ));

      await tester.tap(find.text('Call'));
      await tester.pumpAndSettle();

      expect(find.byType(SnackBar), findsNothing);
    });

    testWidgets('contactBroker handles exception', (tester) async {
        await tester.pumpWidget(createWidgetUnderTest(
            Builder(builder: (context) {
                return ElevatedButton(
                    onPressed: () => ListingUrlLauncher.contactBroker(context, '000'),
                    child: const Text('Call'),
                );
            }),
        ));

        await tester.tap(find.text('Call'));
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 100));

        expect(find.byType(SnackBar), findsOneWidget);
        expect(find.text('Could not launch dialer'), findsOneWidget);
    });

    testWidgets('contactBroker handles failure (launch returns false)', (tester) async {
        await tester.pumpWidget(createWidgetUnderTest(
            Builder(builder: (context) {
                return ElevatedButton(
                    onPressed: () => ListingUrlLauncher.contactBroker(context, '999'),
                    child: const Text('Call'),
                );
            }),
        ));

        await tester.tap(find.text('Call'));
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 100));

        expect(find.byType(SnackBar), findsOneWidget);
        expect(find.text('Could not launch dialer'), findsOneWidget);
    });
  });
}
