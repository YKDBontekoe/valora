import 'dart:async';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:plugin_platform_interface/plugin_platform_interface.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher_platform_interface/url_launcher_platform_interface.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/listing_detail_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/property_photo_service.dart';

@GenerateMocks([ApiService, FavoritesProvider, PropertyPhotoService])
import 'listing_detail_screen_test.mocks.dart';

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
    return true;
  }

  @override
  Future<bool> canLaunch(String url) async => true;
}

class MockHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return _MockHttpClient();
  }
}

class _MockHttpClient extends Mock implements HttpClient {
  @override
  Future<HttpClientRequest> getUrl(Uri url) async {
    return _MockHttpClientRequest();
  }
}

class _MockHttpClientRequest extends Mock implements HttpClientRequest {
  @override
  Future<HttpClientResponse> close() async {
    return _MockHttpClientResponse();
  }
}

class _MockHttpClientResponse extends Mock implements HttpClientResponse {
  final List<int> _imageBytes = [
    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // Signature
    0x00, 0x00, 0x00, 0x0D, // IHDR length
    0x49, 0x48, 0x44, 0x52, // IHDR type
    0x00, 0x00, 0x00, 0x01, // Width
    0x00, 0x00, 0x00, 0x01, // Height
    0x08, 0x06, 0x00, 0x00, 0x00, // Bit depth, color type, compression, filter, interlace
    0x1F, 0x15, 0xC4, 0x89, // CRC
    0x00, 0x00, 0x00, 0x0A, // IDAT length
    0x49, 0x44, 0x41, 0x54, // IDAT type
    0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, // Compressed data
    0x0D, 0x0A, 0x2D, 0xB4, // CRC
    0x00, 0x00, 0x00, 0x00, // IEND length
    0x49, 0x45, 0x4E, 0x44, // IEND type
    0xAE, 0x42, 0x60, 0x82, // CRC
  ];

  @override
  int get statusCode => 200;

  @override
  int get contentLength => _imageBytes.length;

  @override
  HttpClientResponseCompressionState get compressionState =>
      HttpClientResponseCompressionState.notCompressed;

  @override
  StreamSubscription<List<int>> listen(
    void Function(List<int> event)? onData, {
    Function? onError,
    void Function()? onDone,
    bool? cancelOnError,
  }) {
    onData?.call(_imageBytes);
    onDone?.call();
    return Stream<List<int>>.fromIterable([_imageBytes]).listen(null);
  }
}

void main() {
  late MockApiService mockApiService;
  late MockFavoritesProvider mockFavoritesProvider;
  late MockPropertyPhotoService mockPropertyPhotoService;

  setUp(() {
    mockApiService = MockApiService();
    mockFavoritesProvider = MockFavoritesProvider();
    mockPropertyPhotoService = MockPropertyPhotoService();

    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);
    when(mockPropertyPhotoService.getPropertyPhotos(latitude: anyNamed('latitude'), longitude: anyNamed('longitude')))
        .thenReturn([]);

    UrlLauncherPlatform.instance = MockUrlLauncher();
    HttpOverrides.global = MockHttpOverrides();
  });

  Widget createWidgetUnderTest(Listing listing) {
    return MultiProvider(
      providers: [
        Provider<ApiService>.value(value: mockApiService),
        Provider<PropertyPhotoService>.value(value: mockPropertyPhotoService),
        ChangeNotifierProvider<FavoritesProvider>.value(
          value: mockFavoritesProvider,
        ),
      ],
      child: MaterialApp(home: ListingDetailScreen(listing: listing)),
    );
  }

  testWidgets(
    'ListingDetailScreen shows "Contact Broker" button when phone is present',
    (WidgetTester tester) async {
      final listing = Listing(
        id: '1',
        fundaId: '123',
        address: 'Test Address',
        brokerPhone: '+1234567890',
      );

      await tester.pumpWidget(createWidgetUnderTest(listing));
      // Pump frames to allow animations to start and finish
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 800));

      expect(find.text('Contact Broker'), findsOneWidget);
      expect(find.byIcon(Icons.phone_rounded), findsOneWidget);

      // Unmount to clean up any pending timers
      await tester.pumpWidget(const SizedBox());
      await tester.pump(const Duration(milliseconds: 500));
    },
  );

  testWidgets(
    'ListingDetailScreen hides "Contact Broker" button when phone is missing',
    (WidgetTester tester) async {
      final listing = Listing(
        id: '1',
        fundaId: '123',
        address: 'Test Address',
        brokerPhone: null,
      );

      await tester.pumpWidget(createWidgetUnderTest(listing));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 800));

      expect(find.text('Contact Broker'), findsNothing);

      await tester.pumpWidget(const SizedBox());
      await tester.pump(const Duration(milliseconds: 500));
    },
  );

  testWidgets('ListingDetailScreen shows broker section if logo present', (
    WidgetTester tester,
  ) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerLogoUrl: 'http://example.com/logo.png',
      agentName: 'Test Agent',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 800));

    expect(find.text('Broker'), findsOneWidget);
    expect(find.text('Test Agent'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pump(const Duration(milliseconds: 500));
  });

  testWidgets('Tapping "View on Funda" launches URL success', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      url: 'https://example.com',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 800));

    await tester.scrollUntilVisible(find.text('View on Funda'), 500);
    await tester.pump(const Duration(milliseconds: 300)); // Allow scroll settle

    await tester.tap(find.text('View on Funda'));
    await tester.pump(const Duration(milliseconds: 300)); // Allow tap settle

    expect(find.byType(SnackBar), findsNothing);

    await tester.pumpWidget(const SizedBox());
    await tester.pump(const Duration(milliseconds: 500));
  });

  testWidgets('Tapping "View on Funda" handles launch failure', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      url: 'https://fail.com',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 800));

    await tester.scrollUntilVisible(find.text('View on Funda'), 500);
    await tester.pump(const Duration(milliseconds: 300));

    await tester.tap(find.text('View on Funda'));
    // Pump to start snackbar animation
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500)); // Wait for entrance

    expect(find.byType(SnackBar), findsOneWidget);
    expect(find.textContaining('Could not open'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pump(const Duration(milliseconds: 500));
  });

  testWidgets('Tapping "View on Funda" handles launch error', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      url: 'https://error.com',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 800));

    await tester.scrollUntilVisible(find.text('View on Funda'), 500);
    await tester.pump(const Duration(milliseconds: 300));

    await tester.tap(find.text('View on Funda'));
    // Pump to start snackbar animation
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.byType(SnackBar), findsOneWidget);
    expect(find.textContaining('Could not open'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pump(const Duration(milliseconds: 500));
  });

  testWidgets('Tapping "Contact Broker" launches dialer', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerPhone: '+123456',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 800));

    await tester.scrollUntilVisible(find.text('Contact Broker'), 500);
    await tester.pump(const Duration(milliseconds: 300));

    await tester.tap(find.text('Contact Broker'));
    await tester.pump(const Duration(milliseconds: 300));

    expect(find.byType(SnackBar), findsNothing);

    await tester.pumpWidget(const SizedBox());
    await tester.pump(const Duration(milliseconds: 500));
  });

  testWidgets('Lazy loading handles fetch failure gracefully', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      url: 'https://example.com', // Indicates full details fetch needed
      description: null,
    );

    when(mockApiService.getListing('1')).thenThrow(Exception('Fetch error'));

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pump(); // Start fetch
    // Explicitly wait for animations + async work.
    // ValoraAnimations.slow is likely slow, plus intervals. 1000ms should cover it.
    await tester.pump(const Duration(milliseconds: 1000));

    // Should stay on screen and show whatever details we have
    expect(find.text('Test Address'), findsOneWidget);
    // Loading indicator should disappear (after error caught)
    expect(find.byType(LinearProgressIndicator), findsNothing);

    // Unmount to stop any shimmer/animations
    await tester.pumpWidget(const SizedBox());
    await tester.pump(const Duration(milliseconds: 500));
  });

  testWidgets('Lazy loading handles photo enrichment failure gracefully', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      latitude: 52.0,
      longitude: 4.0,
      imageUrls: [],
      imageUrl: null,
    );

    when(mockPropertyPhotoService.getPropertyPhotos(latitude: 52.0, longitude: 4.0))
        .thenThrow(Exception('Photo error'));

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pump(); // Start fetch
    await tester.pump(const Duration(milliseconds: 1000));

    // Should stay on screen
    expect(find.text('Test Address'), findsOneWidget);
    // Loading indicator should disappear
    expect(find.byType(LinearProgressIndicator), findsNothing);

    // Unmount to stop any shimmer/animations
    await tester.pumpWidget(const SizedBox());
    await tester.pump(const Duration(milliseconds: 500));
  });

  testWidgets('Lazy loading does nothing if details already present', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      description: 'Already has description',
      imageUrls: ['http://example.com/img.jpg'],
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    // Initial pump to build
    await tester.pump();
    // Settle any entrance animations (flutter_animate usually runs once)
    // Avoid pumpAndSettle if it times out, use finite duration instead
    await tester.pump(const Duration(milliseconds: 1000));

    // Verify no API calls made
    verifyNever(mockApiService.getListing(any));
    verifyNever(mockPropertyPhotoService.getPropertyPhotos(latitude: anyNamed('latitude'), longitude: anyNamed('longitude')));

    // Unmount to stop any shimmer/animations
    await tester.pumpWidget(const SizedBox());
    await tester.pump(const Duration(milliseconds: 500));
  });
}
