import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:valora_app/services/api_service.dart';

void main() {
  test('getNotifications parses CursorPagedResult correctly', () async {
    final mockClient = MockClient((request) async {
      expect(request.url.path.endsWith('/notifications'), isTrue);
      expect(request.url.queryParameters['limit'], '50');
      expect(request.url.queryParameters['unreadOnly'], 'false');

      final response = {
        'items': [
          {
            'id': '1',
            'title': 'Test',
            'body': 'Body',
            'isRead': false,
            'createdAt': DateTime.now().toIso8601String(),
            'type': 0
          }
        ],
        'nextCursor': 'cursor_123',
        'hasMore': true
      };

      return http.Response(json.encode(response), 200);
    });

    final apiService = ApiService(client: mockClient);
    final result = await apiService.getNotifications();

    expect(result.items.length, 1);
    expect(result.items.first.title, 'Test');
    expect(result.nextCursor, 'cursor_123');
    expect(result.hasMore, true);
  });

  test('getNotifications passes cursor parameter', () async {
    final mockClient = MockClient((request) async {
      expect(request.url.queryParameters['cursor'], 'cursor_abc');
      return http.Response(json.encode({'items': [], 'hasMore': false}), 200);
    });

    final apiService = ApiService(client: mockClient);
    await apiService.getNotifications(cursor: 'cursor_abc');
  });
}
