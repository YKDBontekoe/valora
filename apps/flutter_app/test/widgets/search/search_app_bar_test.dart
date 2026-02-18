import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:valora_app/services/property_photo_service.dart';
import 'package:valora_app/models/listing_response.dart';
import 'package:valora_app/widgets/search/search_app_bar.dart';
import 'package:valora_app/widgets/search/search_input.dart';

@GenerateMocks([ApiService, PropertyPhotoService, PdokService])
import 'search_app_bar_test.mocks.dart';

// Create a Fake instead of Mock for NotificationService to avoid complex setup
class FakeNotificationService extends NotificationService {
  FakeNotificationService() : super(MockApiService());

  @override
  int get unreadCount => 0;
}

void main() {
  late MockApiService mockApiService;
  late MockPropertyPhotoService mockPropertyPhotoService;
  late FakeNotificationService fakeNotificationService;
  late SearchListingsProvider searchProvider;
  late TextEditingController searchController;
  late MockPdokService mockPdokService;

  setUp(() {
    mockApiService = MockApiService();
    mockPropertyPhotoService = MockPropertyPhotoService();
    fakeNotificationService = FakeNotificationService();

    // Setup SearchListingsProvider
    searchProvider = SearchListingsProvider(
      apiService: mockApiService,
      propertyPhotoService: mockPropertyPhotoService,
    );

    searchController = TextEditingController();
    mockPdokService = MockPdokService();
  });

  tearDown(() {
    searchController.dispose();
  });

  Widget createWidget({
    required VoidCallback onSortTap,
    required VoidCallback onFilterTap,
  }) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<SearchListingsProvider>.value(
          value: searchProvider,
        ),
        ChangeNotifierProvider<NotificationService>.value(
          value: fakeNotificationService,
        ),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: CustomScrollView(
            slivers: [
              SearchAppBar(
                searchController: searchController,
                pdokService: mockPdokService,
                onSuggestionSelected: (_) {},
                onSubmitted: () {},
                onSortTap: onSortTap,
                onFilterTap: onFilterTap,
              ),
            ],
          ),
        ),
      ),
    );
  }

  testWidgets('SearchAppBar renders title and icons', (WidgetTester tester) async {
    await tester.pumpWidget(createWidget(onSortTap: () {}, onFilterTap: () {}));
    await tester.pumpAndSettle();

    expect(find.text('Search'), findsOneWidget);
    expect(find.byIcon(Icons.notifications_outlined), findsOneWidget);
    expect(find.byIcon(Icons.sort_rounded), findsOneWidget);
    expect(find.byIcon(Icons.tune_rounded), findsOneWidget);
    expect(find.byType(SearchInput), findsOneWidget);
  });

  testWidgets('SearchAppBar calls onSortTap when sort icon tapped', (WidgetTester tester) async {
    bool sortTapped = false;
    await tester.pumpWidget(createWidget(
      onSortTap: () => sortTapped = true,
      onFilterTap: () {},
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.sort_rounded));
    expect(sortTapped, isTrue);
  });

  testWidgets('SearchAppBar calls onFilterTap when filter icon tapped', (WidgetTester tester) async {
    bool filterTapped = false;
    await tester.pumpWidget(createWidget(
      onSortTap: () {},
      onFilterTap: () => filterTapped = true,
    ));
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.tune_rounded));
    expect(filterTapped, isTrue);
  });

  testWidgets('SearchAppBar shows active filter indicator', (WidgetTester tester) async {
    when(mockApiService.getListings(any)).thenAnswer((_) async => ListingResponse(
      items: [],
      pageIndex: 1,
      totalPages: 1,
      totalCount: 0,
      hasNextPage: false,
      hasPreviousPage: false,
    ));

    // Apply a filter to the provider
    await searchProvider.applyFilters(
      minPrice: 100000,
      maxPrice: null,
      city: null,
      minBedrooms: null,
      minLivingArea: null,
      maxLivingArea: null,
      minSafetyScore: null,
      minCompositeScore: null,
      sortBy: null,
      sortOrder: null,
    );

    await tester.pumpWidget(createWidget(onSortTap: () {}, onFilterTap: () {}));
    await tester.pumpAndSettle();

    // Find the indicator by Key
    final indicatorFinder = find.byKey(const Key('search_filter_indicator'));
    expect(indicatorFinder, findsOneWidget);

    // Verify it has the correct decoration
    final container = tester.widget<Container>(indicatorFinder);
    final decoration = container.decoration as BoxDecoration;
    expect(decoration.color, isNotNull);
    expect(decoration.shape, BoxShape.circle);
  });
}
