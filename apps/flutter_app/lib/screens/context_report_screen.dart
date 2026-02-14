import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../providers/user_profile_provider.dart';
import '../providers/context_report_provider.dart';
import '../services/api_service.dart';
import '../widgets/report/context_report_view.dart';

import '../widgets/valora_widgets.dart';

class ContextReportScreen extends StatefulWidget {
  const ContextReportScreen({super.key});

  @override
  State<ContextReportScreen> createState() => _ContextReportScreenState();
}

class _ContextReportScreenState extends State<ContextReportScreen> {
  final TextEditingController _inputController = TextEditingController();

  @override
  void dispose() {
    _inputController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<ContextReportProvider>(
      create: (_) => ContextReportProvider(apiService: context.read<ApiService>(), initialRadius: context.read<UserProfileProvider>().profile?.defaultRadiusMeters ?? 1000),
      child: Consumer<ContextReportProvider>(
        builder: (context, provider, _) {
          return Scaffold(
            appBar: AppBar(
              title: const Text('Property Analytics'),
              actions: [
                if (provider.report != null)
                  IconButton(
                    tooltip: 'New Report',
                    onPressed: provider.clear,
                    icon: const Icon(Icons.refresh_rounded),
                  ),
              ],
            ),
            body: SafeArea(
              child: provider.report != null
                  ? ListView(
                      padding: const EdgeInsets.all(20),
                      children: [
                        ContextReportView(report: provider.report!),
                      ],
                    )
                  : _InputForm(
                      controller: _inputController,
                      provider: provider,
                    ),
            ),
          );
        },
      ),
    );
  }
}

class _InputForm extends StatelessWidget {
  const _InputForm({
    required this.controller,
    required this.provider,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        // Hero section
        Container(
          padding: const EdgeInsets.all(24),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [
                theme.colorScheme.primaryContainer,
                theme.colorScheme.primaryContainer.withValues(alpha: 0.5),
              ],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                Icons.analytics_rounded,
                size: 48,
                color: theme.colorScheme.primary,
              ),
              const SizedBox(height: 16),
              Text(
                'Neighborhood Analytics',
                style: theme.textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Get comprehensive insights about any Dutch address including demographics, safety, amenities, and environmental data.',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),
        // Search field
        ValueListenableBuilder<TextEditingValue>(
          valueListenable: controller,
          builder: (context, value, _) {
            return TextField(
              controller: controller,
              decoration: InputDecoration(
                hintText: 'Enter address (e.g. Damrak 1 Amsterdam)',
                filled: true,
                fillColor: theme.colorScheme.surfaceContainerLow,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(14),
                  borderSide: BorderSide.none,
                ),
                prefixIcon: const Icon(Icons.search_rounded),
                suffixIcon: value.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear_rounded),
                        onPressed: () => controller.clear(),
                      )
                    : null,
              ),
              textInputAction: TextInputAction.search,
              onSubmitted: (_) => provider.generate(controller.text),
            );
          },
        ),
        const SizedBox(height: 20),
        // Radius slider
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: theme.colorScheme.surfaceContainerLow,
            borderRadius: BorderRadius.circular(14),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Search Radius',
                    style: theme.textTheme.labelLarge,
                  ),
                  Text(
                    '${provider.radiusMeters}m',
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w600,
                      color: theme.colorScheme.primary,
                    ),
                  ),
                ],
              ),
              SliderTheme(
                data: SliderTheme.of(context).copyWith(
                  trackHeight: 6,
                  thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 10),
                ),
                child: Slider(
                  min: 200,
                  max: 5000,
                  divisions: 24,
                  value: provider.radiusMeters.toDouble(),
                  onChanged: (value) => provider.setRadiusMeters(value.round()),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),
        // Generate button
        SizedBox(
          height: 56,
          child: FilledButton.icon(
            onPressed: provider.isLoading
                ? null
                : () => provider.generate(controller.text),
            style: FilledButton.styleFrom(
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(14),
              ),
            ),
            icon: provider.isLoading
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                : const Icon(Icons.search_rounded),
            label: Text(
              provider.isLoading ? 'Analyzing...' : 'Generate Report',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
            ),
          ),
        ),
        if (provider.error != null) ...[
          const SizedBox(height: 24),
          ValoraEmptyState(
            icon: Icons.error_outline_rounded,
            title: 'Report Generation Failed',
            subtitle: provider.error,
            action: ValoraButton(
              label: 'Try Again',
              onPressed: () => provider.generate(controller.text),
            ),
          ),
        ],
        // Recent Searches
        if (provider.history.isNotEmpty) ...[
          const SizedBox(height: 32),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Recent Searches',
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              TextButton(
                onPressed: () => _confirmClearHistory(context, provider),
                child: const Text('Clear All'),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ...provider.history.map((item) {
            return ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.history_rounded),
              title: Text(item.query),
              subtitle: Text(
                _formatDate(item.timestamp),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
              trailing: IconButton(
                icon: const Icon(Icons.close_rounded, size: 20),
                onPressed: () => provider.removeFromHistory(item.query),
              ),
              onTap: () {
                controller.text = item.query;
                provider.generate(item.query);
              },
            );
          }),
        ],
      ],
    );
  }

  Future<void> _confirmClearHistory(
    BuildContext context,
    ContextReportProvider provider,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Clear History?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Clear',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text(
          'Are you sure you want to clear your search history?',
        ),
      ),
    );

    if (confirmed == true) {
      await provider.clearHistory();
    }
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final yesterday = today.subtract(const Duration(days: 1));
    final checkDate = DateTime(date.year, date.month, date.day);

    if (checkDate == today) {
      return 'Today';
    } else if (checkDate == yesterday) {
      return 'Yesterday';
    } else {
      return '${date.day}/${date.month}/${date.year}';
    }
  }
}
