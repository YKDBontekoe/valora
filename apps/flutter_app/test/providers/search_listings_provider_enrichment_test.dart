import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/property_photo_service.dart';

@GenerateMocks([ApiService, PropertyPhotoService])
import 'search_listings_provider_enrichment_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late MockPropertyPhotoService mockPropertyPhotoService;
  late SearchListingsProvider provider;

  setUp(() {
    mockApiService = MockApiService();
    mockPropertyPhotoService = MockPropertyPhotoService();
    provider = SearchListingsProvider(
      apiService: mockApiService,
      propertyPhotoService: mockPropertyPhotoService,
    );
  });

  group('fetchFullListingDetails', () {
    test('returns original listing if it has description', () async {
      final listing = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        description: 'Has description',
        url: 'http://example.com',
      );

      final result = await provider.fetchFullListingDetails(listing);

      expect(result, same(listing));
      verifyZeroInteractions(mockApiService);
    });

    test('returns original listing if it has no url', () async {
      final listing = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        url: null, // No URL means PDOK or no source link
      );

      final result = await provider.fetchFullListingDetails(listing);

      expect(result, same(listing));
      verifyZeroInteractions(mockApiService);
    });

    test('fetches full details if description missing and has url', () async {
      final summary = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        url: 'http://example.com',
      );
      final full = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        description: 'Full details',
        url: 'http://example.com',
      );

      when(mockApiService.getListing('1')).thenAnswer((_) async => full);

      final result = await provider.fetchFullListingDetails(summary);

      expect(result, full);
      verify(mockApiService.getListing('1')).called(1);
    });

    test('returns original listing on API error', () async {
      final summary = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        url: 'http://example.com',
      );

      when(mockApiService.getListing('1')).thenThrow(Exception('API Error'));

      final result = await provider.fetchFullListingDetails(summary);

      expect(result, same(summary));
      verify(mockApiService.getListing('1')).called(1);
    });
  });

  group('enrichListingWithPhotos', () {
    test('returns original listing if it already has photos', () async {
      final listing = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        imageUrls: ['http://example.com/1.jpg'],
      );

      final result = await provider.enrichListingWithPhotos(listing);

      expect(result, same(listing));
      verifyZeroInteractions(mockPropertyPhotoService);
    });

    test('returns original listing if missing lat/lon', () async {
      final listing = Listing(id: '1', fundaId: '1', address: 'Test');

      final result = await provider.enrichListingWithPhotos(listing);

      expect(result, same(listing));
      verifyZeroInteractions(mockPropertyPhotoService);
    });

    test('fetches photos and returns enriched listing', () async {
      final listing = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        latitude: 52.0,
        longitude: 4.0,
      );
      final photos = ['http://pdok.nl/1.png', 'http://pdok.nl/2.png'];

      when(
        mockPropertyPhotoService.getPropertyPhotos(
          latitude: 52.0,
          longitude: 4.0,
        ),
      ).thenReturn(photos);

      final result = await provider.enrichListingWithPhotos(listing);

      expect(result.imageUrls, photos);
      expect(result.imageUrl, photos.first);
      verify(
        mockPropertyPhotoService.getPropertyPhotos(
          latitude: 52.0,
          longitude: 4.0,
        ),
      ).called(1);
    });

    test('returns original listing if no photos found', () async {
      final listing = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        latitude: 52.0,
        longitude: 4.0,
      );

      when(
        mockPropertyPhotoService.getPropertyPhotos(
          latitude: 52.0,
          longitude: 4.0,
        ),
      ).thenReturn([]);

      final result = await provider.enrichListingWithPhotos(listing);

      expect(result, same(listing));
      verify(
        mockPropertyPhotoService.getPropertyPhotos(
          latitude: 52.0,
          longitude: 4.0,
        ),
      ).called(1);
    });

    test('returns original listing on service error', () async {
      final listing = Listing(
        id: '1',
        fundaId: '1',
        address: 'Test',
        latitude: 52.0,
        longitude: 4.0,
      );

      when(
        mockPropertyPhotoService.getPropertyPhotos(
          latitude: 52.0,
          longitude: 4.0,
        ),
      ).thenThrow(Exception('Service Error'));

      final result = await provider.enrichListingWithPhotos(listing);

      expect(result, same(listing));
      verify(
        mockPropertyPhotoService.getPropertyPhotos(
          latitude: 52.0,
          longitude: 4.0,
        ),
      ).called(1);
    });
  });
}
