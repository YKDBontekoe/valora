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

@GenerateMocks([FavoritesProvider, ApiService, PropertyPhotoService])
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
    0x89,
    0x50,
    0x4E,
    0x47,
    0x0D,
    0x0A,
    0x1A,
    0x0A,
    0x00,
    0x00,
    0x00,
    0x0D,
    0x49,
    0x48,
    0x44,
    0x52,
    0x00,
    0x00,
    0x00,
    0x01,
    0x00,
    0x00,
    0x00,
    0x01,
    0x08,
    0x06,
    0x00,
    0x00,
    0x00,
    0x1F,
    0x15,
    0xC4,
    0x89,
    0x00,
    0x00,
    0x00,
    0x0A,
    0x49,
    0x44,
    0x41,
    0x54,
    0x78,
    0x9C,
    0x63,
    0x00,
    0x01,
    0x00,
    0x00,
    0x05,
    0x00,
    0x01,
    0x0D,
    0x0A,
    0x2D,
    0xB4,
    0x00,
    0x00,
    0x00,
    0x00,
    0x49,
    0x45,
    0x4E,
    0x44,
    0xAE,
    0x42,
    0x60,
    0x82,
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
  late MockFavoritesProvider mockFavoritesProvider;
  late MockApiService mockApiService;
  late MockPropertyPhotoService mockPropertyPhotoService;

  setUp(() {
    mockFavoritesProvider = MockFavoritesProvider();
    mockApiService = MockApiService();
    mockPropertyPhotoService = MockPropertyPhotoService();

    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);
    when(mockPropertyPhotoService.getPropertyPhotos(latitude: anyNamed('latitude'), longitude: anyNamed('longitude'))).thenReturn([]);

    UrlLauncherPlatform.instance = MockUrlLauncher();
    HttpOverrides.global = MockHttpOverrides();
  });

  Widget createWidgetUnderTest(Listing listing) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>.value(
          value: mockFavoritesProvider,
        ),
        Provider<ApiService>.value(value: mockApiService),
        Provider<PropertyPhotoService>.value(value: mockPropertyPhotoService),
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
      await tester.pumpAndSettle();

      expect(find.text('Contact Broker'), findsOneWidget);
      expect(find.byIcon(Icons.phone_rounded), findsOneWidget);
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
      await tester.pumpAndSettle();

      expect(find.text('Contact Broker'), findsNothing);
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
    await tester.pumpAndSettle();

    expect(find.text('Broker'), findsOneWidget);
    expect(find.text('Test Agent'), findsOneWidget);
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
    await tester.pump(const Duration(milliseconds: 500));

    await tester.scrollUntilVisible(find.text('View on Funda'), 500);
    await tester.pumpAndSettle();

    await tester.tap(find.text('View on Funda'));
    await tester.pumpAndSettle();

    expect(find.byType(SnackBar), findsNothing);
  });

  testWidgets('Tapping "View on Funda" handles launch failure', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      url: 'https://fail.com',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(find.text('View on Funda'), 500);
    await tester.pumpAndSettle();

    await tester.tap(find.text('View on Funda'));
    // Pump to start snackbar animation, but don't settle (which might wait for it to close)
    await tester.pump();
    await tester.pump(
      const Duration(milliseconds: 500),
    ); // Wait a bit for entrance

    expect(find.byType(SnackBar), findsOneWidget);
    expect(find.textContaining('Could not open'), findsOneWidget);
  });

  testWidgets('Tapping "View on Funda" handles launch error', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      url: 'https://error.com',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(find.text('View on Funda'), 500);
    await tester.pumpAndSettle();

    await tester.tap(find.text('View on Funda'));
    // Pump to start snackbar animation, but don't settle
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.byType(SnackBar), findsOneWidget);
    expect(find.textContaining('Could not open'), findsOneWidget);
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

    await tester.scrollUntilVisible(find.text('Contact Broker'), 500);
    await tester.pumpAndSettle();

    await tester.tap(find.text('Contact Broker'));
    await tester.pumpAndSettle();

    expect(find.byType(SnackBar), findsNothing);
  });

  testWidgets('Fetches full details if listing is a summary', (tester) async {
    final summaryListing = Listing(
      id: 'summary-1',
      fundaId: '123',
      address: 'Summary Address',
      url: 'https://example.com/123',
      description: null,
      features: {},
    );

    final fullListing = Listing(
      id: 'summary-1',
      fundaId: '123',
      address: 'Summary Address',
      url: 'https://example.com/123',
      description: 'Full description loaded',
      features: {'Key': 'Value'},
    );

    when(mockApiService.getListing('summary-1')).thenAnswer((_) async => fullListing);

    await tester.pumpWidget(createWidgetUnderTest(summaryListing));
    // Initial pump
    await tester.pump();
    // Run async logic in addPostFrameCallback
    await tester.pump(Duration.zero);

    // Wait for async fetch
    verify(mockApiService.getListing('summary-1')).called(1);

    // Rebuild with new data
    await tester.pump();

    // Settle animations
    await tester.pumpAndSettle();

    expect(find.text('Full description loaded'), findsOneWidget);
  });

  testWidgets('Does not fetch details if listing is already full', (tester) async {
    final fullListing = Listing(
      id: 'full-1',
      fundaId: '123',
      address: 'Full Address',
      url: 'https://example.com/123',
      description: 'Already has description',
      features: {},
    );

    await tester.pumpWidget(createWidgetUnderTest(fullListing));
    await tester.pumpAndSettle();

    verifyNever(mockApiService.getListing(any));
    expect(find.text('Already has description'), findsOneWidget);
  });

  testWidgets('Enriches with photos if coordinates are present', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Address',
      latitude: 52.0,
      longitude: 4.0,
      imageUrls: [],
      description: 'Desc',
      features: {'f': 'v'},
    );

    when(mockPropertyPhotoService.getPropertyPhotos(latitude: 52.0, longitude: 4.0))
        .thenReturn(['http://photo1.com', 'http://photo2.com']);

    await tester.pumpWidget(createWidgetUnderTest(listing));
    // Initial pump
    await tester.pump();
    // Run async logic in addPostFrameCallback
    await tester.pump(Duration.zero);

    verify(mockPropertyPhotoService.getPropertyPhotos(latitude: 52.0, longitude: 4.0)).called(1);

    // Settle animations (use timeout to be safe against infinite animations if any)
    await tester.pump(const Duration(seconds: 2));
  });
}
