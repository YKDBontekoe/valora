import 'dart:convert';
import '../models/ai_chat_message.dart';
import '../models/ai_conversation.dart';
import 'api_client.dart';

class AiService {
  final ApiClient _apiClient;

  AiService({required ApiClient apiClient}) : _apiClient = apiClient;

  Future<List<AiConversation>> getHistory() async {
    final response = await _apiClient.get('/api/ai/history');

    return _apiClient.handleResponse(response, (body) {
      final List<dynamic> data = json.decode(body);
      return data.map((json) => AiConversation.fromJson(json)).toList();
    });
  }

  Future<List<AiChatMessage>> getMessages(String conversationId) async {
    final response = await _apiClient.get('/api/ai/history/$conversationId');

    return _apiClient.handleResponse(response, (body) {
      final data = json.decode(body);
      final List<dynamic> messages = data['messages'] ?? [];
      return messages.map((json) => AiChatMessage.fromJson(json)).toList();
    });
  }

  Future<void> deleteConversation(String conversationId) async {
    final response = await _apiClient.delete('/api/ai/history/$conversationId');

    if (response.statusCode != 204 && response.statusCode != 200) {
      await _apiClient.handleResponse(response, (_) => null);
    }
  }

  Future<Map<String, dynamic>> sendMessage({
    String? conversationId,
    required String prompt,
    String intent = 'chat',
    List<AiChatMessage>? history,
    Map<String, dynamic>? contextReport,
  }) async {
    final data = {
      'conversationId': conversationId,
      'prompt': prompt,
      'intent': intent,
      'history': history?.map((msg) => msg.toJson()).toList(),
      'contextReport': contextReport,
    };

    final response = await _apiClient.post('/api/ai/chat', data: data);

    return _apiClient.handleResponse(response, (body) => json.decode(body));
  }

  /// No-op dispose as ApiClient is managed externally.
  void dispose() {}
}
