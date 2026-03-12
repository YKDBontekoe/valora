import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/ai_chat_provider.dart';
import '../../widgets/ai_chat/ai_chat_message_bubble.dart';

class AiChatScreen extends StatefulWidget {
  const AiChatScreen({super.key});

  @override
  State<AiChatScreen> createState() => _AiChatScreenState();
}

class _AiChatScreenState extends State<AiChatScreen> {
  final TextEditingController _textController = TextEditingController();
  final ScrollController _scrollController = ScrollController();

  @override
  void dispose() {
    _textController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _scrollToBottom() {
    if (_scrollController.hasClients) {
      _scrollController.animateTo(
        _scrollController.position.maxScrollExtent,
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeOut,
      );
    }
  }

  void _handleSubmitted(String text) {
    if (text.trim().isEmpty) return;
    _textController.clear();
    context.read<AiChatProvider>().sendMessage(text);
    // Delay to allow the list to update before scrolling
    Future.delayed(const Duration(milliseconds: 100), _scrollToBottom);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Valora AI Assistant'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () {
              context.read<AiChatProvider>().startNewConversation();
            },
            tooltip: 'New Conversation',
          ),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: Selector<AiChatProvider, ({List<dynamic> activeMessages, bool isSending, String? error})>(
              selector: (context, provider) => (
                activeMessages: provider.activeMessages,
                isSending: provider.isSending,
                error: provider.error,
              ),
              builder: (context, data, child) {
                final messages = data.activeMessages;

                if (messages.isEmpty && !data.isSending) {
                  return const Center(
                    child: Text('How can I help you today?'),
                  );
                }

                return Column(
                  children: [
                    Expanded(
                      child: ListView.builder(
                        controller: _scrollController,
                        itemCount: messages.length,
                        itemBuilder: (context, index) {
                          final isLast = index == messages.length - 1;
                          final isError = isLast && data.error != null;
                          return AiChatMessageBubble(
                            message: messages[index],
                            isError: isError,
                            onRetry: isError ? () {
                              context.read<AiChatProvider>().retryLastMessage();
                              Future.delayed(const Duration(milliseconds: 100), _scrollToBottom);
                            } : null,
                          );
                        },
                      ),
                    ),
                    if (data.error != null)
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(12),
                        color: theme.colorScheme.errorContainer,
                        child: Row(
                          children: [
                            Icon(Icons.error_outline, color: theme.colorScheme.error),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(
                                data.error!,
                                style: theme.textTheme.bodySmall?.copyWith(
                                  color: theme.colorScheme.onErrorContainer,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                  ],
                );
              },
            ),
          ),
          Selector<AiChatProvider, bool>(
            selector: (context, provider) => provider.isSending,
            builder: (context, isSending, child) {
              if (isSending) {
                return const Padding(
                  padding: EdgeInsets.all(8.0),
                  child: LinearProgressIndicator(),
                );
              }
              return const SizedBox.shrink();
            },
          ),
          Container(
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              boxShadow: [
                BoxShadow(
                  offset: const Offset(0, -2),
                  blurRadius: 4,
                  color: Colors.black.withValues(alpha: 0.05),
                ),
              ],
            ),
            child: SafeArea(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8.0, vertical: 8.0),
                child: Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: _textController,
                        decoration: InputDecoration(
                          hintText: 'Ask about a neighborhood, report, or property...',
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(24),
                            borderSide: BorderSide.none,
                          ),
                          filled: true,
                          fillColor: theme.colorScheme.surfaceContainerHighest,
                          contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
                        ),
                        textInputAction: TextInputAction.send,
                        onSubmitted: _handleSubmitted,
                        maxLines: null,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Selector<AiChatProvider, bool>(
                      selector: (context, provider) => provider.isSending,
                      builder: (context, isSending, child) {
                        return IconButton(
                          icon: const Icon(Icons.send),
                          color: theme.colorScheme.primary,
                          onPressed: isSending
                              ? null
                              : () => _handleSubmitted(_textController.text),
                        );
                      }
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
