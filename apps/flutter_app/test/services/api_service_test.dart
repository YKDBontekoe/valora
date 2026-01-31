import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart'; // Standard http testing package
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/models/listing_response.dart';

void main() {
  group('ApiService', () {
    test('getListings returns ListingResponse if the http call completes successfully', () async {
      final mockClient = MockClient((request) async {
        if (request.url.toString().startsWith('http://localhost:5000/api/listings')) {
          return http.Response(
              '''
              {
                "items": [{"id": "00000000-0000-0000-0000-000000000000", "fundaId": "1", "address": "Test", "city": "Test", "postalCode": "1234AB", "price": 100000, "bedrooms": 2, "bathrooms": 1, "livingAreaM2": 100, "plotAreaM2": 100, "propertyType": "House", "status": "Available", "url": "http://test", "imageUrl": "http://test", "listedDate": "2023-01-01T00:00:00Z", "createdAt": "2023-01-01T00:00:00Z"}],
                "pageIndex": 1,
                "totalPages": 1,
                "totalCount": 1,
                "hasNextPage": false,
                "hasPreviousPage": false
              }
              ''',
              200);
        }
        return http.Response('Not Found', 404);
      });

      final apiService = ApiService(client: mockClient);
      final response = await apiService.getListings();

      expect(response, isA<ListingResponse>());
      expect(response.items.length, 1);
      expect(response.items[0].address, 'Test');
    });

    test('getListings throws exception on 500 error', () async {
      final mockClient = MockClient((request) async {
        return http.Response('Server Error', 500);
      });

      final apiService = ApiService(client: mockClient);
      expect(apiService.getListings(), throwsException);
    });

    test('getListings throws Exception on SocketException', () {
      final mockClient = MockClient((request) async {
        throw const SocketException('No internet');
      });

      final apiService = ApiService(client: mockClient);
      expect(apiService.getListings(), throwsException);
    });
  });
}
