import 'dart:convert';

import '../../../models/notification.dart';

class NotificationsApiMapper {
  const NotificationsApiMapper._();

  static List<ValoraNotification> parseNotifications(String body) {
    final List<dynamic> jsonList = json.decode(body) as List<dynamic>;
    return jsonList
        .map((dynamic item) => ValoraNotification.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  static int parseUnreadCount(String body) {
    final Map<String, dynamic> jsonBody = json.decode(body) as Map<String, dynamic>;
    return jsonBody['count'] as int;
  }
}
