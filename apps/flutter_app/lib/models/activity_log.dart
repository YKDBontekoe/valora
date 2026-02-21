class ActivityLog {
  final String id;
  final String actorId;
  final String type;
  final String summary;
  final DateTime createdAt;
  final String? metadata;

  ActivityLog({
    required this.id,
    required this.actorId,
    required this.type,
    required this.summary,
    required this.createdAt,
    this.metadata,
  });

  factory ActivityLog.fromJson(Map<String, dynamic> json) {
    return ActivityLog(
      id: json['id'],
      actorId: json['actorId'],
      type: json['type'],
      summary: json['summary'],
      createdAt: DateTime.parse(json['createdAt']),
      metadata: json['metadata'],
    );
  }
}
