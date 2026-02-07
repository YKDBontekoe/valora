enum NotificationType {
  info,
  priceDrop,
  newListing,
  system,
}

class ValoraNotification {
  final String id;
  final String title;
  final String body;
  final bool isRead;
  final DateTime createdAt;
  final String? actionUrl;
  final NotificationType type;

  ValoraNotification({
    required this.id,
    required this.title,
    required this.body,
    required this.isRead,
    required this.createdAt,
    this.actionUrl,
    required this.type,
  });

  factory ValoraNotification.fromJson(Map<String, dynamic> json) {
    final int typeIndex = json['type'] as int;
    final NotificationType type = (typeIndex >= 0 && typeIndex < NotificationType.values.length)
        ? NotificationType.values[typeIndex]
        : NotificationType.info;

    return ValoraNotification(
      id: json['id'] as String,
      title: json['title'] as String,
      body: json['body'] as String,
      isRead: json['isRead'] as bool,
      createdAt: DateTime.parse(json['createdAt'] as String),
      actionUrl: json['actionUrl'] as String?,
      type: type,
    );
  }
}
