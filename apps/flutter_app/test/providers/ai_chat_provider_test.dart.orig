import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/models/ai_chat_message.dart';
import 'package:valora_app/models/ai_conversation.dart';
import 'package:valora_app/services/ai_service.dart';
import 'package:valora_app/providers/ai_chat_provider.dart';

@GenerateNiceMocks([MockSpec<AiService>()])
import 'ai_chat_provider_test.mocks.dart';

void main() {
  late AiChatProvider provider;
  late MockAiService mockAiService;

  setUp(() {
    mockAiService = MockAiService();
    provider = AiChatProvider(mockAiService);
  });

  test('loadHistory updates state correctly', () async {
    final conversations = [
      AiConversation(id: '1', title: 'Test 1', updatedAtUtc: DateTime.now()),
    ];
    when(mockAiService.getHistory()).thenAnswer((_) async => conversations);

    expect(provider.isLoadingHistory, isFalse);

    final future = provider.loadHistory();
    expect(provider.isLoadingHistory, isTrue);

    await future;

    expect(provider.isLoadingHistory, isFalse);
    expect(provider.conversations, equals(conversations));
    expect(provider.error, isNull);
  });

  test('loadHistory handles errors', () async {
    when(mockAiService.getHistory()).thenThrow(Exception('API Error'));

    await provider.loadHistory();

    expect(provider.isLoadingHistory, isFalse);
    expect(provider.error, contains('API Error'));
  });

  test('loadConversation updates active state', () async {
    final messages = [
      AiChatMessage(role: 'user', content: 'hello'),
    ];
    when(mockAiService.getMessages('1')).thenAnswer((_) async => messages);

    await provider.loadConversation('1');

    expect(provider.activeConversationId, equals('1'));
    expect(provider.activeMessages, equals(messages));
    expect(provider.error, isNull);
  });

  test('deleteConversation updates list and clears active if matching', () async {
    final conv = AiConversation(id: '1', title: 'Test', updatedAtUtc: DateTime.now());
    when(mockAiService.getHistory()).thenAnswer((_) async => [conv]);
    await provider.loadHistory();

    await provider.loadConversation('1');
    expect(provider.activeConversationId, equals('1'));

    await provider.deleteConversation('1');

    expect(provider.conversations, isEmpty);
    expect(provider.activeConversationId, isNull);
    expect(provider.activeMessages, isEmpty);
  });

  test('sendMessage optimistic update and success', () async {
    when(mockAiService.sendMessage(
      prompt: anyNamed('prompt'),
      conversationId: anyNamed('conversationId'),
      history: anyNamed('history'),
      contextReport: anyNamed('contextReport'),
    )).thenAnswer((_) async => {'response': 'Hi there!', 'conversationId': '1'});

    await provider.sendMessage('Hello');

    expect(provider.activeMessages.length, equals(2));
    expect(provider.activeMessages[0].content, equals('Hello'));
    expect(provider.activeMessages[1].content, equals('Hi there!'));
    expect(provider.activeConversationId, equals('1'));
  });

  test('sendMessage handles failure and retry', () async {
    when(mockAiService.sendMessage(
      prompt: anyNamed('prompt'),
    )).thenThrow(Exception('Send Error'));

    await provider.sendMessage('Hello');

    expect(provider.activeMessages, isEmpty);
    expect(provider.error, contains('Send Error'));
    expect(provider.lastFailedPrompt, equals('Hello'));

    // Setup success for retry
    when(mockAiService.sendMessage(
      prompt: anyNamed('prompt'),
    )).thenAnswer((_) async => {'response': 'Success', 'conversationId': '1'});

    provider.retryLastMessage();
    // Allow async to settle
    await Future.delayed(Duration.zero);

    expect(provider.activeMessages.length, equals(2));
    expect(provider.activeMessages[1].content, equals('Success'));
  });
}
