import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/ai_chat_message.dart';
import '../models/ai_conversation.dart';
import 'auth_service.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

class AiService {
  final http.Client _client;
  final AuthService _authService;

  AiService({http.Client? client, required AuthService authService})
      : _client = client ?? http.Client(),
        _authService = authService;

  String get _baseUrl {
    return dotenv.env['API_BASE_URL'] ?? 'http://localhost:5001';
  }

  Future<Map<String, String>> _getHeaders() async {
    final token = await _authService.getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Future<List<AiConversation>> getHistory() async {
    final response = await _client.get(
      Uri.parse('$_baseUrl/api/ai/history'),
      headers: await _getHeaders(),
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => AiConversation.fromJson(json)).toList();
    } else {
      throw Exception('Failed to load conversation history');
    }
  }

  Future<List<AiChatMessage>> getMessages(String conversationId) async {
    final response = await _client.get(
      Uri.parse('$_baseUrl/api/ai/history/$conversationId'),
      headers: await _getHeaders(),
    );

    if (response.statusCode == 200) {
      final data = json.decode(response.body);
      final List<dynamic> messages = data['messages'] ?? [];
      return messages.map((json) => AiChatMessage.fromJson(json)).toList();
    } else if (response.statusCode == 404) {
      throw Exception('Conversation not found');
    } else {
      throw Exception('Failed to load messages');
    }
  }

  Future<void> deleteConversation(String conversationId) async {
    final response = await _client.delete(
      Uri.parse('$_baseUrl/api/ai/history/$conversationId'),
      headers: await _getHeaders(),
    );

    if (response.statusCode != 204) {
      throw Exception('Failed to delete conversation');
    }
  }

  Future<Map<String, dynamic>> sendMessage({
    String? conversationId,
    required String prompt,
    String intent = 'chat',
    List<AiChatMessage>? history,
    Map<String, dynamic>? contextReport,
  }) async {
    final body = {
      if (conversationId != null) 'conversationId': conversationId,
      'prompt': prompt,
      'intent': intent,
      if (history != null && history.isNotEmpty)
        'history': history.map((msg) => msg.toJson()).toList(),
      if (contextReport != null) 'contextReport': contextReport,
    };

    final response = await _client.post(
      Uri.parse('$_baseUrl/api/ai/chat'),
      headers: await _getHeaders(),
      body: json.encode(body),
    );

    if (response.statusCode == 200) {
      return json.decode(response.body);
    } else {
      final errorData = json.decode(response.body);
      throw Exception(errorData['detail'] ?? 'Failed to send message');
    }
  }
}
