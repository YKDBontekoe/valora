class Comment {
  final String id;
  final String userId;
  final String content;
  final DateTime createdAt;
  final String? parentId;
  final List<Comment> replies;
  final Map<String, List<String>> reactions;

  Comment({
    required this.id,
    required this.userId,
    required this.content,
    required this.createdAt,
    this.parentId,
    required this.replies,
    required this.reactions,
  });

  factory Comment.fromJson(Map<String, dynamic> json) {
    var repliesList = (json['replies'] as List?)?.map((e) => Comment.fromJson(e)).toList() ?? [];
    var reactionsMap = (json['reactions'] as Map<String, dynamic>?)?.map(
        (key, value) => MapEntry(key, (value as List).cast<String>())
      ) ?? {};

    return Comment(
      id: json['id'],
      userId: json['userId'],
      content: json['content'],
      createdAt: DateTime.parse(json['createdAt']),
      parentId: json['parentId'],
      replies: repliesList,
      reactions: reactionsMap,
    );
  }
}
