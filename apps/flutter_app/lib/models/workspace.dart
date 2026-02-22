enum WorkspaceRole { owner, editor, viewer }

class Workspace {
  final String id;
  final String name;
  final String? description;
  final String ownerId;
  final DateTime createdAt;
  final int memberCount;
  final int savedListingCount;

  Workspace({
    required this.id,
    required this.name,
    this.description,
    required this.ownerId,
    required this.createdAt,
    required this.memberCount,
    required this.savedListingCount,
  });

  factory Workspace.fromJson(Map<String, dynamic> json) {
    return Workspace(
      id: json['id'],
      name: json['name'],
      description: json['description'],
      ownerId: json['ownerId'],
      createdAt: DateTime.parse(json['createdAt']),
      memberCount: json['memberCount'],
      savedListingCount: json['savedListingCount'],
    );
  }
}

class WorkspaceMember {
  final String id;
  final String? userId;
  final String? email;
  final WorkspaceRole role;
  final bool isPending;
  final DateTime? joinedAt;

  WorkspaceMember({
    required this.id,
    this.userId,
    this.email,
    required this.role,
    required this.isPending,
    this.joinedAt,
  });

  factory WorkspaceMember.fromJson(Map<String, dynamic> json) {
    return WorkspaceMember(
      id: json['id'],
      userId: json['userId'],
      email: json['email'],
      role: WorkspaceRole.values.firstWhere(
        (e) => e.name.toLowerCase() == (json['role'] as String).toLowerCase(),
        orElse: () => WorkspaceRole.viewer,
      ),
      isPending: json['isPending'],
      joinedAt: json['joinedAt'] != null
          ? DateTime.parse(json['joinedAt'])
          : null,
    );
  }
}
