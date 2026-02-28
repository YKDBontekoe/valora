class AiConversation {
  final String id;
  final String title;
  final DateTime updatedAtUtc;

  AiConversation({
    required this.id,
    required this.title,
    required this.updatedAtUtc,
  });

  factory AiConversation.fromJson(Map<String, dynamic> json) {
    return AiConversation(
      id: json['id'] as String,
      title: json['title'] as String,
      updatedAtUtc: DateTime.parse(json['updatedAtUtc'] as String),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      'updatedAtUtc': updatedAtUtc.toIso8601String(),
    };
  }
}
