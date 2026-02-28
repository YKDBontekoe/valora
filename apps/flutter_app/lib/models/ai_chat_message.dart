class AiChatMessage {
  final String role;
  final String content;
  final DateTime? createdAtUtc;

  AiChatMessage({
    required this.role,
    required this.content,
    this.createdAtUtc,
  });

  factory AiChatMessage.fromJson(Map<String, dynamic> json) {
    return AiChatMessage(
      role: json['role'] as String,
      content: json['content'] as String,
      createdAtUtc: json['createdAtUtc'] != null
          ? DateTime.parse(json['createdAtUtc'] as String)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'role': role,
      'content': content,
      if (createdAtUtc != null) 'createdAtUtc': createdAtUtc!.toIso8601String(),
    };
  }
}
