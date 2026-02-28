import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/models/ai_chat_message.dart';
import 'package:valora_app/models/ai_conversation.dart';
import 'package:valora_app/services/ai_service.dart';
import 'package:valora_app/services/api_client.dart';

@GenerateNiceMocks([MockSpec<ApiClient>()])
import 'ai_service_test.mocks.dart';

void main() {
  late AiService aiService;
  late MockApiClient mockApiClient;

  setUp(() async {
    mockApiClient = MockApiClient();
    aiService = AiService(apiClient: mockApiClient);
  });

  test('sendMessage handles history mapping correctly', () async {
    when(mockApiClient.post(
      any,
      data: anyNamed('data'),
    )).thenAnswer((_) async => http.Response('{"response": "ok", "conversationId": "c1"}', 200));

    // For handleResponse we need a real ApiClient or stub it. 
    // Since we mock ApiClient, we must stub handleResponse too if it's called.
    // However, handleResponse is a method on ApiClient that AiService calls.
    
    when(mockApiClient.handleResponse<Map<String, dynamic>>(any, any))
        .thenAnswer((Invocation inv) async {
          final response = inv.positionalArguments[0] as http.Response;
          final parser = inv.positionalArguments[1] as Map<String, dynamic> Function(String);
          return parser(response.body);
        });

    final date = DateTime.utc(2023);
    final response = await aiService.sendMessage(
      prompt: 'test',
      history: [
        AiChatMessage(role: 'user', content: 'hello', createdAtUtc: date),
      ],
      contextReport: {'key': 'value'},
    );

    expect(response['response'], 'ok');

    final captured = verify(mockApiClient.post(
      any,
      data: captureAnyNamed('data'),
    )).captured;

    final Map<String, dynamic> data = captured.first;
    expect(data['history'], isNotNull);
    expect(data['history'][0]['role'], 'user');
    expect(data['history'][0]['createdAtUtc'], date.toIso8601String());
    expect(data['contextReport'], isNotNull);
  });

  test('sendMessage handles success', () async {
    when(mockApiClient.post(any, data: anyNamed('data')))
        .thenAnswer((_) async => http.Response('{"response": "ok", "conversationId": "c1"}', 200));
    
    when(mockApiClient.handleResponse<Map<String, dynamic>>(any, any))
        .thenAnswer((Invocation inv) async {
          final response = inv.positionalArguments[0] as http.Response;
          final parser = inv.positionalArguments[1] as Map<String, dynamic> Function(String);
          return parser(response.body);
        });

    final response = await aiService.sendMessage(prompt: 'test');
    expect(response['response'], 'ok');
    expect(response['conversationId'], 'c1');
  });

  test('getHistory handles success', () async {
    when(mockApiClient.get(any))
        .thenAnswer((_) async => http.Response('[{"id": "1", "title": "Chat", "updatedAtUtc": "2023-01-01T00:00:00Z"}]', 200));

    when(mockApiClient.handleResponse<List<AiConversation>>(any, any))
        .thenAnswer((Invocation inv) async {
          final response = inv.positionalArguments[0] as http.Response;
          final parser = inv.positionalArguments[1] as List<AiConversation> Function(String);
          return parser(response.body);
        });

    final history = await aiService.getHistory();
    expect(history.length, 1);
    expect(history[0].id, '1');
  });

  test('getMessages handles success', () async {
    when(mockApiClient.get(any))
        .thenAnswer((_) async => http.Response('{"messages": [{"role": "user", "content": "hi"}]}', 200));

    when(mockApiClient.handleResponse<List<AiChatMessage>>(any, any))
        .thenAnswer((Invocation inv) async {
          final response = inv.positionalArguments[0] as http.Response;
          final parser = inv.positionalArguments[1] as List<AiChatMessage> Function(String);
          return parser(response.body);
        });

    final msgs = await aiService.getMessages('1');
    expect(msgs.length, 1);
    expect(msgs[0].role, 'user');
    expect(msgs[0].content, 'hi');
  });

  test('deleteConversation handles success', () async {
    when(mockApiClient.delete(any))
        .thenAnswer((_) async => http.Response('', 204));

    await aiService.deleteConversation('1');
    verify(mockApiClient.delete(any)).called(1);
  });
}
