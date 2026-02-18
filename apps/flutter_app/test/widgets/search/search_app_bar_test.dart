import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:valora_app/services/property_photo_service.dart';
import 'package:valora_app/widgets/search/search_app_bar.dart';
import 'package:valora_app/widgets/search/search_input.dart';

@GenerateMocks([ApiService, PropertyPhotoService])
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
  late PdokService pdokService;

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
    pdokService = PdokService();
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
                pdokService: pdokService,
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

    // The indicator is a Container with primary color.
    // We can verify its existence by checking for the Container within the Stack of the filter button.
    final filterButtonStack = find.ancestor(
      of: find.byIcon(Icons.tune_rounded),
      matching: find.byType(Stack),
    );

    // Find the indicator: A positioned container with box decoration color primary
    // This is a bit implementation detail specific, but valid for unit testing the widget's visual state logic
    expect(
      find.descendant(
        of: filterButtonStack,
        matching: find.byWidgetPredicate((widget) =>
          widget is Container &&
          widget.decoration is BoxDecoration &&
          (widget.decoration as BoxDecoration).color != null
        ),
      ),
      findsOneWidget,
    );
  });
}
