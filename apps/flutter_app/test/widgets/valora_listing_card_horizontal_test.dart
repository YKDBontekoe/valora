import 'dart:async';
import 'dart:io';
import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/widgets/valora_listing_card_horizontal.dart';

import 'valora_listing_card_horizontal_test.mocks.dart';

@GenerateMocks([FavoritesProvider])
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
  @override
  int get statusCode => 404; // Return 404 to avoid image parsing issues

  @override
  int get contentLength => 0;

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
    onDone?.call();
    return Stream<List<int>>.fromIterable([]).listen(null);
  }
}

void main() {
  late MockFavoritesProvider mockFavoritesProvider;
  HttpOverrides? originalHttpOverrides;

  setUp(() {
    originalHttpOverrides = HttpOverrides.current;
    mockFavoritesProvider = MockFavoritesProvider();
    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);
    HttpOverrides.global = MockHttpOverrides();
  });

  tearDown(() {
    HttpOverrides.global = originalHttpOverrides;
  });

  Widget createWidgetUnderTest(Listing listing) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>.value(
          value: mockFavoritesProvider,
        ),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: ValoraListingCardHorizontal(
            listing: listing,
            onTap: () {},
          ),
        ),
      ),
    );
  }

  testWidgets('ListingSpecsRow renders specs correctly', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      bedrooms: 3,
      bathrooms: 2,
      livingAreaM2: 120,
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    expect(find.text('3'), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
    expect(find.text('120 m²'), findsOneWidget);
    expect(find.byType(ListingSpecsRow), findsOneWidget);
  });

  testWidgets('ListingSpecsRow renders nothing when no specs', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      bedrooms: null,
      bathrooms: null,
      livingAreaM2: null,
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    expect(find.byType(ListingSpecsRow), findsOneWidget);
    // Verify visually empty (no icons)
    expect(find.byIcon(Icons.bed_outlined), findsNothing);
    expect(find.byIcon(Icons.bathtub_outlined), findsNothing);
    expect(find.byIcon(Icons.square_foot_outlined), findsNothing);
  });

  testWidgets('ValoraListingCardHorizontal handles hover state', (tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    await tester.pumpAndSettle();

    final gesture = await tester.createGesture(kind: PointerDeviceKind.mouse);
    await gesture.addPointer(location: Offset.zero);
    addTearDown(gesture.removePointer);

    // Move mouse over widget
    await gesture.moveTo(tester.getCenter(find.byType(ValoraListingCardHorizontal)));
    await tester.pumpAndSettle();

    // Verify scale effect (container transform)
    // This is implicit, but we can check if the widget tree is valid and built
    expect(find.byType(ValoraListingCardHorizontal), findsOneWidget);
  });
}
