enum ActivityLogType {
  workspaceCreated,
  memberInvited,
  memberJoined,
  memberRemoved,
  listingSaved,
  listingRemoved,
  commentAdded,
  commentReplied,
  roleChanged,
  workspaceDeleted,
  workspaceUpdated,
  unknown
}

class ActivityLog {
  final String id;
  final String actorId;
  final ActivityLogType type;
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
      type: ActivityLogType.values.firstWhere(
        (e) => e.name.toLowerCase() == (json['type'] as String).toLowerCase(),
        orElse: () => ActivityLogType.unknown,
      ),
      summary: json['summary'],
      createdAt: DateTime.parse(json['createdAt']),
      metadata: json['metadata'],
    );
  }
}
