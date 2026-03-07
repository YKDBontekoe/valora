import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/ai_chat_message.dart';
import 'package:valora_app/providers/ai_chat_provider.dart';
import 'package:valora_app/screens/ai_chat/ai_chat_screen.dart';
import 'package:valora_app/widgets/ai_chat/ai_chat_message_bubble.dart';

import 'ai_chat_screen_test.mocks.dart';

@GenerateNiceMocks([MockSpec<AiChatProvider>()])
void main() {
  late MockAiChatProvider mockProvider;

  setUp(() {
    mockProvider = MockAiChatProvider();
    when(mockProvider.activeMessages).thenReturn([]);
    when(mockProvider.isSending).thenReturn(false);
    when(mockProvider.error).thenReturn(null);
  });

  Widget createWidgetUnderTest() {
    return MaterialApp(
      home: ChangeNotifierProvider<AiChatProvider>.value(
        value: mockProvider,
        child: const AiChatScreen(),
      ),
    );
  }

  testWidgets('renders empty state initially', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('How can I help you today?'), findsOneWidget);
    expect(find.byType(TextField), findsOneWidget);
    expect(find.byIcon(Icons.send), findsOneWidget);
  });

  testWidgets('renders messages when activeMessages is populated', (tester) async {
    final messages = <AiChatMessage>[
      AiChatMessage(
        role: 'user',
        content: 'Hello AI',
        createdAtUtc: DateTime.now().toUtc(),
      ),
      AiChatMessage(
        role: 'assistant',
        content: 'Hi there!',
        createdAtUtc: DateTime.now().toUtc(),
      ),
    ];
    when(mockProvider.activeMessages).thenReturn(messages);

    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('Hello AI'), findsOneWidget);
    expect(find.text('Hi there!'), findsOneWidget);
    expect(find.byType(AiChatMessageBubble), findsNWidgets(2));
  });

  testWidgets('shows loading indicator when isSending is true', (tester) async {
    when(mockProvider.isSending).thenReturn(true);

    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.byType(LinearProgressIndicator), findsOneWidget);
  });

  testWidgets('shows error message when error is not null', (tester) async {
    final messages = <AiChatMessage>[
      AiChatMessage(
        role: 'user',
        content: 'Failed message',
        createdAtUtc: DateTime.now().toUtc(),
      ),
    ];
    when(mockProvider.activeMessages).thenReturn(messages);
    when(mockProvider.error).thenReturn('Network Error');

    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('Network Error'), findsOneWidget);
  });

  testWidgets('tapping send calls sendMessage', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    await tester.enterText(find.byType(TextField), 'Test message');
    await tester.tap(find.byIcon(Icons.send));
    await tester.pumpAndSettle(); // Allows the scroll delayed timer to complete

    verify(mockProvider.sendMessage('Test message')).called(1);
  });

  testWidgets('tapping new conversation calls startNewConversation', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    await tester.tap(find.byIcon(Icons.add));
    await tester.pump();

    verify(mockProvider.startNewConversation()).called(1);
  });
}
