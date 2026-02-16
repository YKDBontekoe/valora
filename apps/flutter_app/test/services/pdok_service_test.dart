import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:valora_app/services/pdok_service.dart';

void main() {
  group('PdokService Tests', () {
    test('search returns suggestions on 200', () async {
      final mockClient = MockClient((request) async {
        return http.Response(json.encode({
          'response': {
            'docs': [
              {'id': '1', 'type': 'adres', 'weergavenaam': 'Damrak 1', 'score': 10.0}
            ]
          }
        }), 200);
      });

      final service = PdokService(client: mockClient);
      final results = await service.search('Damrak');

      expect(results.length, 1);
      expect(results[0].displayName, 'Damrak 1');
      expect(results[0].id, '1');
      expect(results[0].type, 'adres');
    });

    test('reverseLookup returns address on 200', () async {
      final mockClient = MockClient((request) async {
        return http.Response(json.encode({
          'response': {
            'docs': [
              {'weergavenaam': 'Damrak 1, Amsterdam'}
            ]
          }
        }), 200);
      });

      final service = PdokService(client: mockClient);
      final result = await service.reverseLookup(52.37, 4.89);

      expect(result, 'Damrak 1, Amsterdam');
    });

    test('reverseLookup returns null on empty docs', () async {
      final mockClient = MockClient((request) async {
        return http.Response(json.encode({
          'response': {'docs': []}
        }), 200);
      });

      final service = PdokService(client: mockClient);
      final result = await service.reverseLookup(0, 0);

      expect(result, isNull);
    });

    test('search returns empty list on error', () async {
      final mockClient = MockClient((request) async {
        return http.Response('Error', 500);
      });

      final service = PdokService(client: mockClient);
      final results = await service.search('test');

      expect(results, isEmpty);
    });

    test('search returns empty list on exception', () async {
      final mockClient = MockClient((request) async {
        throw Exception('Network fail');
      });

      final service = PdokService(client: mockClient);
      final results = await service.search('test');

      expect(results, isEmpty);
    });

    test('reverseLookup returns null on exception', () async {
      final mockClient = MockClient((request) async {
        throw Exception('Network fail');
      });

      final service = PdokService(client: mockClient);
      final result = await service.reverseLookup(0, 0);

      expect(result, isNull);
    });
  });
}
