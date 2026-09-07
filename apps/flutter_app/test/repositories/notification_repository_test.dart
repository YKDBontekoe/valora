import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/repositories/notification_repository.dart';
import 'package:valora_app/services/api_client.dart';

@GenerateMocks([ApiClient])
import 'notification_repository_test.mocks.dart';

void main() {
  late NotificationRepository repository;
  late MockApiClient mockClient;

  setUp(() {
    mockClient = MockApiClient();
    repository = NotificationRepository(mockClient);
  });

  group('NotificationRepository', () {
    test('getNotifications parses list correctly', () async {
      final jsonBody = jsonEncode([
        {'id': '1', 'title': 'Test', 'body': 'Body', 'isRead': false, 'createdAt': DateTime.now().toIso8601String(), 'type': 'Info'}
      ]);
      final response = http.Response(jsonBody, 200);

      when(mockClient.get(
        '/notifications',
        queryParameters: anyNamed('queryParameters'),
      )).thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.getNotifications();

      expect(result.length, 1);
      expect(result.first.title, 'Test');
    });

    test('getUnreadNotificationCount parses count correctly', () async {
      final jsonBody = jsonEncode({'count': 5});
      final response = http.Response(jsonBody, 200);

      when(mockClient.get('/notifications/unread-count'))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final count = await repository.getUnreadNotificationCount();

      expect(count, 5);
    });

    test('markNotificationAsRead calls correct endpoint', () async {
      final response = http.Response('', 204);

      when(mockClient.post('/notifications/1/read'))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((_) async => null);

      await repository.markNotificationAsRead('1');

      verify(mockClient.post('/notifications/1/read')).called(1);
    });

    test('markAllNotificationsAsRead calls correct endpoint', () async {
      final response = http.Response('', 204);

      when(mockClient.post('/notifications/read-all'))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((_) async => null);

      await repository.markAllNotificationsAsRead();

      verify(mockClient.post('/notifications/read-all')).called(1);
    });

    test('deleteNotification calls correct endpoint', () async {
      final response = http.Response('', 204);

      when(mockClient.delete('/notifications/1'))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((_) async => null);

      await repository.deleteNotification('1');

      verify(mockClient.delete('/notifications/1')).called(1);
    });
  });
}
