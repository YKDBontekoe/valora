import 'package:flutter/material.dart';
import '../../models/ai_chat_message.dart';

class AiChatMessageBubble extends StatelessWidget {
  final AiChatMessage message;
  final bool isError;
  final VoidCallback? onRetry;

  const AiChatMessageBubble({
    super.key,
    required this.message,
    this.isError = false,
    this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    final isUser = message.role == 'user';
    final theme = Theme.of(context);

    return Align(
      alignment: isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: Column(
        crossAxisAlignment: isUser ? CrossAxisAlignment.end : CrossAxisAlignment.start,
        children: [
          Container(
            margin: const EdgeInsets.symmetric(vertical: 4, horizontal: 16),
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: isUser
                  ? theme.colorScheme.primary
                  : theme.colorScheme.surfaceContainerHighest,
              borderRadius: BorderRadius.only(
                topLeft: const Radius.circular(16),
                topRight: const Radius.circular(16),
                bottomLeft: isUser ? const Radius.circular(16) : Radius.zero,
                bottomRight: isUser ? Radius.zero : const Radius.circular(16),
              ),
              border: isError ? Border.all(color: theme.colorScheme.error) : null,
            ),
            child: Text(
              message.content,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: isUser
                    ? theme.colorScheme.onPrimary
                    : theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          if (isError && onRetry != null)
            Padding(
              padding: const EdgeInsets.only(right: 16, bottom: 8),
              child: TextButton.icon(
                onPressed: onRetry,
                icon: Icon(Icons.refresh, size: 16, color: theme.colorScheme.error),
                label: Text(
                  'Retry',
                  style: theme.textTheme.labelSmall?.copyWith(color: theme.colorScheme.error),
                ),
                style: TextButton.styleFrom(
                  minimumSize: Size.zero,
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
              ),
            ),
        ],
      ),
    );
  }
}
