import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/ai_chat_message.dart';
import 'package:valora_app/models/ai_conversation.dart';

void main() {
  test('AiChatMessage fromJson and toJson', () {
    final date = DateTime.utc(2023, 1, 1);
    final msg = AiChatMessage(role: 'user', content: 'hello', createdAtUtc: date);

    final json = msg.toJson();
    expect(json['role'], 'user');
    expect(json['content'], 'hello');
    expect(json['createdAtUtc'], date.toIso8601String());

    final decoded = AiChatMessage.fromJson(json);
    expect(decoded.role, 'user');
    expect(decoded.content, 'hello');
    expect(decoded.createdAtUtc, date);
  });

  test('AiConversation fromJson and toJson', () {
    final date = DateTime.utc(2023, 1, 1);
    final conv = AiConversation(id: '1', title: 'Test', updatedAtUtc: date);

    final json = conv.toJson();
    expect(json['id'], '1');
    expect(json['title'], 'Test');
    expect(json['updatedAtUtc'], date.toIso8601String());

    final decoded = AiConversation.fromJson(json);
    expect(decoded.id, '1');
    expect(decoded.title, 'Test');
    expect(decoded.updatedAtUtc, date);
  });
}
