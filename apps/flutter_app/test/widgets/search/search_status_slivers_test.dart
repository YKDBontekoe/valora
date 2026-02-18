import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/listing_response.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/property_photo_service.dart';
import 'package:valora_app/widgets/search/search_status_slivers.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

@GenerateMocks([ApiService, PropertyPhotoService])
import 'search_status_slivers_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late MockPropertyPhotoService mockPropertyPhotoService;
  late SearchListingsProvider searchProvider;

  setUp(() {
    mockApiService = MockApiService();
    mockPropertyPhotoService = MockPropertyPhotoService();
    searchProvider = SearchListingsProvider(
      apiService: mockApiService,
      propertyPhotoService: mockPropertyPhotoService,
    );
  });

  Widget createWidget({required Widget sliver}) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<SearchListingsProvider>.value(
          value: searchProvider,
        ),
      ],
      child: MaterialApp(
        home: Scaffold(body: CustomScrollView(slivers: [sliver])),
      ),
    );
  }

  group('SearchLoadingSliver', () {
    testWidgets('renders loading indicator when loading', (
      WidgetTester tester,
    ) async {
      when(mockApiService.getListings(any)).thenAnswer((_) async {
        await Future.delayed(const Duration(seconds: 1));
        return ListingResponse(items: [], pageIndex: 1, totalPages: 1, totalCount: 0, hasNextPage: false, hasPreviousPage: false);
      });

      await tester.pumpWidget(createWidget(sliver: const SearchLoadingSliver()));

      searchProvider.refresh();
      await tester.pump();

      expect(find.byType(ValoraLoadingIndicator), findsOneWidget);
      expect(find.text('Searching...'), findsOneWidget);

      await tester.pump(const Duration(seconds: 1));
      await tester.pumpAndSettle();
    });

    testWidgets('renders nothing when not loading', (WidgetTester tester) async {
      await tester.pumpWidget(createWidget(sliver: const SearchLoadingSliver()));
      await tester.pumpAndSettle();

      expect(find.byType(ValoraLoadingIndicator), findsNothing);
    });
  });

  group('SearchErrorSliver', () {
    testWidgets('renders error state when error present and listings empty', (
      WidgetTester tester,
    ) async {
      when(mockApiService.getListings(any)).thenThrow(Exception('Fail'));

      await tester.pumpWidget(createWidget(sliver: const SearchErrorSliver()));

      searchProvider.setQuery('fail');
      await searchProvider.refresh();
      await tester.pumpAndSettle();

      expect(find.byType(ValoraEmptyState), findsOneWidget);
      expect(find.text('Search Failed'), findsOneWidget);
      expect(find.text('Failed to search listings'), findsOneWidget);
    });

    testWidgets('renders nothing when no error', (WidgetTester tester) async {
      await tester.pumpWidget(createWidget(sliver: const SearchErrorSliver()));
      await tester.pumpAndSettle();

      expect(find.byType(ValoraEmptyState), findsNothing);
    });
  });

  group('SearchEmptySliver', () {
    testWidgets('renders "Find your home" when query is empty (initial state)', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(createWidget(sliver: const SearchEmptySliver()));
      await tester.pumpAndSettle();

      expect(find.text('Find your home'), findsOneWidget);
      expect(find.byIcon(Icons.search_rounded), findsOneWidget);
    });

    testWidgets('renders "No results found" when query active but empty results', (
      WidgetTester tester,
    ) async {
      when(mockApiService.getListings(any)).thenAnswer((_) async =>
        ListingResponse(items: [], pageIndex: 1, totalPages: 1, totalCount: 0, hasNextPage: false, hasPreviousPage: false)
      );

      await tester.pumpWidget(createWidget(sliver: const SearchEmptySliver()));

      searchProvider.setQuery('empty');
      await searchProvider.refresh();
      await tester.pumpAndSettle();

      expect(find.text('No results found'), findsOneWidget);
      expect(find.byIcon(Icons.search_off_rounded), findsOneWidget);
    });
  });
}
