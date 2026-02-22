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
    Uri? uri;
    try {
      uri = Uri.parse('${ApiClient.baseUrl}/notifications').replace(
        queryParameters: {
          'unreadOnly': unreadOnly.toString(),
          'limit': limit.toString(),
          'offset': offset.toString(),
        },
      );

      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.get(uri!, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );

      return await _client.handleResponse(
        response,
        (body) => _client.runner(_parseNotifications, body),
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, uri);
    }
  }

  static List<ValoraNotification> _parseNotifications(String body) {
    final List<dynamic> jsonList = json.decode(body);
    return jsonList.map((e) => ValoraNotification.fromJson(e)).toList();
  }

  Future<int> getUnreadNotificationCount() async {
    final uri = Uri.parse('${ApiClient.baseUrl}/notifications/unread-count');
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.get(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );

      return _client.handleResponse(
        response,
        (body) {
          final jsonBody = json.decode(body);
          return jsonBody['count'] as int;
        },
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, uri);
    }
  }

  Future<void> markNotificationAsRead(String id) async {
    final uri = Uri.parse('${ApiClient.baseUrl}/notifications/$id/read');
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.post(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );
      _client.handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _client.handleException(e, stack, uri);
    }
  }

  Future<void> markAllNotificationsAsRead() async {
    final uri = Uri.parse('${ApiClient.baseUrl}/notifications/read-all');
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.post(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );
      _client.handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _client.handleException(e, stack, uri);
    }
  }

  Future<void> deleteNotification(String id) async {
    final uri = Uri.parse('${ApiClient.baseUrl}/notifications/$id');
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.delete(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );
      _client.handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _client.handleException(e, stack, uri);
    }
  }
}
