import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:retry/retry.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:valora_app/core/config/app_config.dart';

import 'api_service_test.mocks.dart';

@GenerateMocks([http.Client])
void main() {
  late ApiService apiService;
  late MockClient mockClient;

  setUpAll(() async {
    await dotenv.load(fileName: ".env.example");
  });

  setUp(() {
    mockClient = MockClient();
    apiService = ApiService(
      client: mockClient,
      retryOptions: const RetryOptions(maxAttempts: 1), // Disable retries for faster tests
    );
  });

  group('ApiService', () {
    test('getListing returns Listing on success', () async {
      final listingId = 'test-id';
      final listingJson = {
        'id': listingId,
        'address': 'Test Address',
        'price': 100000,
        // Add required fields to match Listing model
        'description': 'Test Description',
        'city': 'Test City',
        'zipCode': '1234AB',
        'latitude': 52.0,
        'longitude': 5.0,
        'squareMeters': 100,
        'bedrooms': 2,
        'energyLabel': 'A',
        'propertyType': 'Apartment',
        'constructionYear': 2020,
        'hasGarden': false,
        'hasBalcony': true,
        'hasGarage': false,
        'status': 'ForSale',
        'createdAt': DateTime.now().toIso8601String(),
        'updatedAt': DateTime.now().toIso8601String(),
        'amenities': [],
        'images': []
      };
      final responseBody = json.encode(listingJson);

      when(mockClient.get(
        Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        headers: anyNamed('headers'),
      )).thenAnswer((_) async => http.Response(responseBody, 200));

      final result = await apiService.executeRequest<Listing>(
        (headers) => mockClient.get(
          Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
          headers: headers,
        ),
        (body) => Listing.fromJson(json.decode(body)),
        Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
      );

      expect(result, isA<Listing>());
      expect(result.id, listingId);
    });

    test('executeRequest throws ValidationException on 400', () async {
      final listingId = 'test-id';
      final errorJson = {'detail': 'Invalid ID'};
      final responseBody = json.encode(errorJson);

      when(mockClient.get(
        Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        headers: anyNamed('headers'),
      )).thenAnswer((_) async => http.Response(responseBody, 400));

      expect(
        () => apiService.executeRequest<Listing>(
          (headers) => mockClient.get(
            Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
            headers: headers,
          ),
          (body) => Listing.fromJson(json.decode(body)),
          Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        ),
        throwsA(isA<ValidationException>()),
      );
    });

    test('executeRequest throws NotFoundException on 404', () async {
      final listingId = 'test-id';
      final errorJson = {'detail': 'Not Found'};
      final responseBody = json.encode(errorJson);

      when(mockClient.get(
        Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        headers: anyNamed('headers'),
      )).thenAnswer((_) async => http.Response(responseBody, 404));

      expect(
        () => apiService.executeRequest<Listing>(
          (headers) => mockClient.get(
            Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
            headers: headers,
          ),
          (body) => Listing.fromJson(json.decode(body)),
          Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        ),
        throwsA(isA<NotFoundException>()),
      );
    });

    test('executeRequest throws ServerException on 500', () async {
      final listingId = 'test-id';
      final errorJson = {'detail': 'Server Error'};
      final responseBody = json.encode(errorJson);

      when(mockClient.get(
        Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        headers: anyNamed('headers'),
      )).thenAnswer((_) async => http.Response(responseBody, 500));

      expect(
        () => apiService.executeRequest<Listing>(
          (headers) => mockClient.get(
            Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
            headers: headers,
          ),
          (body) => Listing.fromJson(json.decode(body)),
          Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        ),
        throwsA(isA<ServerException>()),
      );
    });

    test('executeRequest handles legacy array error format', () async {
      final listingId = 'test-id';
      final errorJson = [
        {'property': 'Id', 'error': 'Invalid ID format'}
      ];
      final responseBody = json.encode(errorJson);

      when(mockClient.get(
        Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        headers: anyNamed('headers'),
      )).thenAnswer((_) async => http.Response(responseBody, 400));

      try {
        await apiService.executeRequest<Listing>(
          (headers) => mockClient.get(
            Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
            headers: headers,
          ),
          (body) => Listing.fromJson(json.decode(body)),
          Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        );
        fail('Should throw ValidationException');
      } on ValidationException catch (e) {
        expect(e.message, contains('Invalid ID format'));
      }
    });

    test('executeRequest handles FluentValidation dictionary error format', () async {
      final listingId = 'test-id';
      final errorJson = {
        'errors': {
          'Id': ['Invalid ID']
        }
      };
      final responseBody = json.encode(errorJson);

      when(mockClient.get(
        Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        headers: anyNamed('headers'),
      )).thenAnswer((_) async => http.Response(responseBody, 400));

      try {
        await apiService.executeRequest<Listing>(
          (headers) => mockClient.get(
            Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
            headers: headers,
          ),
          (body) => Listing.fromJson(json.decode(body)),
          Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        );
        fail('Should throw ValidationException');
      } on ValidationException catch (e) {
        expect(e.message, contains('Invalid ID'));
      }
    });

    test('executeRequest throws NetworkException on SocketException', () async {
      final listingId = 'test-id';

      when(mockClient.get(
        Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        headers: anyNamed('headers'),
      )).thenThrow(const SocketException('No Internet'));

      expect(
        () => apiService.executeRequest<Listing>(
          (headers) => mockClient.get(
            Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
            headers: headers,
          ),
          (body) => Listing.fromJson(json.decode(body)),
          Uri.parse('${ApiService.baseUrl}/listings/$listingId'),
        ),
        throwsA(isA<NetworkException>()),
      );
    });
  });
}
