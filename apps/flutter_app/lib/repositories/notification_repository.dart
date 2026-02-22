import 'dart:convert';
import '../models/notification.dart';
import '../services/api_client.dart';

class NotificationRepository {
  final ApiClient _client;

  NotificationRepository(this._client);

  Future<List<ValoraNotification>> getNotifications({
    bool unreadOnly = false,
    int limit = 50,
    int offset = 0,
  }) async {
    final response = await _client.get(
      '/notifications',
      queryParameters: {
        'unreadOnly': unreadOnly.toString(),
        'limit': limit.toString(),
        'offset': offset.toString(),
      },
    );

    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => ValoraNotification.fromJson(e)).toList();
      },
    );
  }

  Future<int> getUnreadNotificationCount() async {
    final response = await _client.get('/notifications/unread-count');
    return _client.handleResponse(
      response,
      (body) {
        final jsonBody = json.decode(body);
        return jsonBody['count'] as int;
      },
    );
  }

  Future<void> markNotificationAsRead(String id) async {
    final response = await _client.post('/notifications/$id/read');
    await _client.handleResponse(response, (_) => null);
  }

  Future<void> markAllNotificationsAsRead() async {
    final response = await _client.post('/notifications/read-all');
    await _client.handleResponse(response, (_) => null);
  }

  Future<void> deleteNotification(String id) async {
    final response = await _client.delete('/notifications/$id');
    await _client.handleResponse(response, (_) => null);
  }
}
