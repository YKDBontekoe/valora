import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../../models/context_report.dart';
import '../../providers/context_report_provider.dart';
import '../common/valora_button.dart';
import '../common/valora_card.dart';
import '../common/valora_shimmer.dart';

class AiInsightCard extends StatelessWidget {
  const AiInsightCard({super.key, required this.report});

  final ContextReport report;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final provider = context.watch<ContextReportProvider>();
    final location = report.location.displayAddress;

    final analysis = provider.getAiInsight(location);
    final isLoading = provider.isAiInsightLoading(location);
    final error = provider.getAiInsightError(location);

    if (analysis != null) {
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
                const Spacer(),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: theme.colorScheme.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    '${analysis.confidence}% Confidence',
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: theme.colorScheme.primary,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            _MarkdownText(
              key: const Key('ai-summary-text'),
              text: analysis.summary,
            ),
            if (analysis.topPositives.isNotEmpty) ...[
              const SizedBox(height: 16),
              Text(
                'Positives',
                style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              ...analysis.topPositives.map((p) => _Point(icon: Icons.check_circle, color: Colors.green, text: p)),
            ],
            if (analysis.topConcerns.isNotEmpty) ...[
              const SizedBox(height: 16),
              Text(
                'Things to Watch',
                style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              ...analysis.topConcerns.map((c) => _Point(icon: Icons.warning, color: Colors.orange, text: c)),
            ],
             if (analysis.disclaimer.isNotEmpty) ...[
              const SizedBox(height: 16),
              Text(
                analysis.disclaimer,
                style: theme.textTheme.labelSmall?.copyWith(
                  color: theme.colorScheme.onSurface.withValues(alpha: 0.6),
                  fontStyle: FontStyle.italic,
                ),
              ),
            ],
          ],
        ),
      ).animate().fadeIn().scale(alignment: Alignment.topCenter);
    }

    if (isLoading) {
      return const ValoraShimmer(
        width: double.infinity,
        height: 150,
        borderRadius: 16,
      );
    }

    if (error != null) {
      return ValoraCard(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Text(
              'Failed to generate insight',
              style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.error),
            ),
            const SizedBox(height: 8),
            ValoraButton(
              label: 'Retry',
              onPressed: () => provider.generateAiInsight(report),
              variant: ValoraButtonVariant.secondary,
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
              onPressed: () => provider.generateAiInsight(report),
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

class _Point extends StatelessWidget {
  const _Point({required this.icon, required this.color, required this.text});

  final IconData icon;
  final Color color;
  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 16, color: color),
          const SizedBox(width: 8),
          Expanded(
            child: Text(text, style: theme.textTheme.bodySmall),
          ),
        ],
      ),
    );
  }
}
