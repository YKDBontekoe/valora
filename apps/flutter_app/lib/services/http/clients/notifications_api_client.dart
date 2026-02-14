import '../../../models/notification.dart';
import '../core/http_transport.dart';
import '../mappers/notifications_api_mapper.dart';

class NotificationsApiClient {
  NotificationsApiClient({required HttpTransport transport}) : _transport = transport;

  final HttpTransport _transport;

  Future<List<ValoraNotification>> getNotifications({
    bool unreadOnly = false,
    int limit = 50,
    int offset = 0,
  }) {
    final Uri uri = Uri.parse('${_transport.baseUrl}/notifications').replace(
      queryParameters: <String, String>{
        'unreadOnly': unreadOnly.toString(),
        'limit': limit.toString(),
        'offset': offset.toString(),
      },
    );

    return _transport.get<List<ValoraNotification>>(
      uri: uri,
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        NotificationsApiMapper.parseNotifications,
      ),
    );
  }

  Future<int> getUnreadNotificationCount() {
    final Uri uri = Uri.parse('${_transport.baseUrl}/notifications/unread-count');

    return _transport.get<int>(
      uri: uri,
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        NotificationsApiMapper.parseUnreadCount,
      ),
    );
  }

  Future<void> markNotificationAsRead(String id) {
    final Uri uri = Uri.parse('${_transport.baseUrl}/notifications/$id/read');
    return _transport.post<void>(
      uri: uri,
      responseHandler: (response) => _transport.parseOrThrow(response, (_) => null),
    );
  }

  Future<void> markAllNotificationsAsRead() {
    final Uri uri = Uri.parse('${_transport.baseUrl}/notifications/read-all');
    return _transport.post<void>(
      uri: uri,
      responseHandler: (response) => _transport.parseOrThrow(response, (_) => null),
    );
  }

  Future<void> deleteNotification(String id) {
    final Uri uri = Uri.parse('${_transport.baseUrl}/notifications/$id');
    return _transport.delete<void>(
      uri: uri,
      responseHandler: (response) => _transport.parseOrThrow(response, (_) => null),
    );
  }
}
