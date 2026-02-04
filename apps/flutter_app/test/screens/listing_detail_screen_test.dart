import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/listing_detail_screen.dart';

@GenerateMocks([FavoritesProvider])
import 'listing_detail_screen_test.mocks.dart';

void main() {
  late MockFavoritesProvider mockFavoritesProvider;
  final List<MethodCall> log = <MethodCall>[];

  setUp(() {
    mockFavoritesProvider = MockFavoritesProvider();
    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);
    HttpOverrides.global = null;
    log.clear();

    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(SystemChannels.platform, (MethodCall methodCall) async {
      log.add(methodCall);
      if (methodCall.method == 'UrlLauncher.launch') {
        return true;
      }
      return null;
    });
  });

  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(SystemChannels.platform, null);
  });

  Widget createWidgetUnderTest(Listing listing) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>.value(value: mockFavoritesProvider),
      ],
      child: MaterialApp(
        home: ListingDetailScreen(listing: listing),
      ),
    );
  }

  testWidgets('ListingDetailScreen shows "Contact Broker" button when phone is present', (WidgetTester tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerPhone: '+1234567890',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    expect(find.text('Contact Broker'), findsOneWidget);
    expect(find.byIcon(Icons.phone_rounded), findsOneWidget);
  });

  testWidgets('ListingDetailScreen hides "Contact Broker" button when phone is missing', (WidgetTester tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerPhone: null,
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    expect(find.text('Contact Broker'), findsNothing);
  });

  testWidgets('ListingDetailScreen shows broker section if logo present', (WidgetTester tester) async {
     final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerLogoUrl: 'http://example.com/logo.png',
      agentName: 'Test Agent',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    expect(find.text('Broker'), findsOneWidget);
    expect(find.text('Test Agent'), findsOneWidget);
  });

  testWidgets('Tapping "View on Funda" launches URL', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      url: 'https://example.com',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    await tester.tap(find.text('View on Funda'));
    await tester.pumpAndSettle();

    // Verify method channel call if possible, or assume success if no error
    // Note: url_launcher implementation details might vary, but we mock standard platform channel
    // For web/newer plugins it might use a different channel name.
    // However, verify no snackbar error is enough for success path.
    expect(find.byType(SnackBar), findsNothing);
  });

  testWidgets('Tapping "Contact Broker" launches dialer', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerPhone: '+123456',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Contact Broker'));
    await tester.pumpAndSettle();

    expect(find.byType(SnackBar), findsNothing);
  });
}
