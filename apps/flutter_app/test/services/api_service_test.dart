import 'dart:convert';
import 'dart:io';

import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  group('ApiService', () {
    test('getListings throws ServerException on 500', () async {
      final client = MockClient((request) async {
        return http.Response('Internal Server Error', 500);
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(),
        throwsA(isA<ServerException>()),
      );
    });

    test('getListings throws NetworkException on SocketException', () async {
      final client = MockClient((request) async {
        throw const SocketException('No internet');
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(),
        throwsA(isA<NetworkException>()),
      );
    });

    test('getListings throws ValidationException on 400', () async {
      final client = MockClient((request) async {
        return http.Response(json.encode({'title': 'Invalid input'}), 400);
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(),
        throwsA(isA<ValidationException>()),
      );
    });

    test('getListings returns data on 200', () async {
      final mockResponse = {
        'items': [],
        'pageIndex': 1,
        'totalPages': 1,
        'totalCount': 0,
        'hasNextPage': false,
        'hasPreviousPage': false
      };

      final client = MockClient((request) async {
        return http.Response(json.encode(mockResponse), 200);
      });

      final apiService = ApiService(client: client);

      final result = await apiService.getListings();
      expect(result.items, isEmpty);
    });
  });
}
