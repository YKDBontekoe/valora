import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../../core/exceptions/app_exceptions.dart';
import '../../models/context_report.dart';
import '../../services/api_service.dart';
import '../common/valora_button.dart';
import '../common/valora_card.dart';
import '../common/valora_shimmer.dart';

class AiInsightCard extends StatefulWidget {
  const AiInsightCard({super.key, required this.report});

  final ContextReport report;

  @override
  State<AiInsightCard> createState() => _AiInsightCardState();
}

class _AiInsightCardState extends State<AiInsightCard> {
  bool _isLoading = false;
  String? _summary;
  String? _error;

  Future<void> _generateInsight() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final apiService = context.read<ApiService>();
      final summary = await apiService.getAiAnalysis(widget.report);
      if (mounted) {
        setState(() {
          _summary = summary;
          _isLoading = false;
        });
      }
    } catch (e, stack) {
      debugPrintStack(label: 'Error generating insight: $e', stackTrace: stack);
      if (mounted) {
        String message = 'Failed to generate insight. Please try again.';
        if (e is AppException) {
          message = e.message;
        }
        setState(() {
          _error = message;
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (_summary != null) {
      return ValoraCard(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.auto_awesome, color: theme.colorScheme.primary),
                const SizedBox(width: 8),
                Text(
                  'AI Insight',
                  style: theme.textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: theme.colorScheme.primary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            _MarkdownText(
              key: const Key('ai-summary-text'),
              text: _summary!,
            ),
          ],
        ),
      ).animate().fadeIn().scale(alignment: Alignment.topCenter);
    }

    if (_isLoading) {
      return const ValoraShimmer(
        width: double.infinity,
        height: 150,
        borderRadius: 16,
      );
    }

    if (_error != null) {
      return ValoraCard(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Row(
              children: [
                Icon(Icons.error_outline_rounded, color: theme.colorScheme.error),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _error!,
                    style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.error),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ValoraButton(
              label: 'Retry',
              onPressed: _generateInsight,
              variant: ValoraButtonVariant.secondary,
              isFullWidth: true,
            ),
          ],
        ),
      );
    }

    return ValoraCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          Row(
            children: [
              Icon(Icons.auto_awesome, color: theme.colorScheme.primary),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Unlock Neighborhood Insights',
                      style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
                    ),
                    Text(
                      'Get an AI-powered summary of the pros & cons.',
                      style: theme.textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: ValoraButton(
              label: 'Generate Insight',
              onPressed: _generateInsight,
              icon: Icons.auto_awesome,
            ),
          ),
        ],
      ),
    );
  }
}

class _MarkdownText extends StatelessWidget {
  const _MarkdownText({super.key, required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final parts = text.split('**');

    return RichText(
      text: TextSpan(
        style: theme.textTheme.bodyMedium?.copyWith(
          color: theme.colorScheme.onSurface,
          height: 1.5,
        ),
        children: List.generate(parts.length, (index) {
          final isBold = index % 2 != 0;
          return TextSpan(
            text: parts[index],
            style: isBold ? const TextStyle(fontWeight: FontWeight.bold) : null,
          );
        }),
      ),
    );
  }
}
