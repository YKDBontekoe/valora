import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../core/theme/valora_spacing.dart';
import '../../../services/pdok_service.dart';
import '../../../providers/context_report_provider.dart';
import '../widgets/search_field.dart';
import '../widgets/radius_selector.dart';
import '../widgets/generate_button.dart';
import '../widgets/quick_actions.dart';
import '../widgets/history_section.dart';
import '../widgets/saved_searches_section.dart';

class SearchLayout extends StatelessWidget {
  const SearchLayout({
    super.key,
    required this.inputController,
    required this.provider,
    required this.pdokService,
  });

  final TextEditingController inputController;
  final ContextReportProvider provider;
  final PdokService pdokService;

  @override
  Widget build(BuildContext context) {
    return CustomScrollView(
      slivers: [
        // Minimal top spacing for status bar
        SliverToBoxAdapter(
          child: SizedBox(
            height: MediaQuery.of(context).padding.top + ValoraSpacing.lg,
          ),
        ),

        // Hero section
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Branding row
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(ValoraSpacing.sm),
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          colors: [
                            Theme.of(context).colorScheme.primary,
                            Theme.of(context).colorScheme.primaryContainer,
                          ],
                          begin: Alignment.bottomLeft,
                          end: Alignment.topRight,
                        ),
                        borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
                        boxShadow: [
                          BoxShadow(
                            color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.25),
                            blurRadius: 16,
                            offset: const Offset(0, 8),
                            spreadRadius: 2,
                          ),
                        ],
                      ),
                      child: Icon(
                        Icons.analytics_rounded,
                        size: 26,
                        color: Theme.of(context).colorScheme.onPrimary,
                      ),
                    ).animate(onPlay: (controller) => controller.repeat(reverse: true))
                     .scale(
                       begin: const Offset(1, 1),
                       end: const Offset(1.05, 1.05),
                       duration: 2000.ms,
                       curve: Curves.easeInOutSine,
                     ),
                    const SizedBox(width: ValoraSpacing.md),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Property Analytics',
                          style: ValoraTypography.headlineSmall.copyWith(
                            fontWeight: FontWeight.w800,
                            letterSpacing: -0.3,
                          ),
                        ),
                        Text(
                          'Neighborhood insights for any Dutch address',
                          style: ValoraTypography.bodySmall.copyWith(
                            color: Theme.of(context).colorScheme.onSurfaceVariant,
                          ),
                        ),
                      ],
                    ),
                  ],
                ).animate().fadeIn(duration: 400.ms).slideX(begin: -0.05),
                const SizedBox(height: ValoraSpacing.xl),

                // Search field
                SearchField(
                  controller: inputController,
                  provider: provider,
                  pdokService: pdokService,
                ).animate().fadeIn(duration: 400.ms, delay: 100.ms).slideY(begin: 0.1),

                const SizedBox(height: ValoraSpacing.md),

                // Radius selector
                const RadiusSelector()
                    .animate()
                    .fadeIn(duration: 400.ms, delay: 200.ms)
                    .slideY(begin: 0.1),

                const SizedBox(height: ValoraSpacing.md),

                // Generate button
                GenerateButton(
                  controller: inputController,
                  provider: provider,
                ).animate().fadeIn(duration: 400.ms, delay: 300.ms).slideY(begin: 0.1),

                // Error state
                if (provider.error != null) ...[
                  const SizedBox(height: ValoraSpacing.lg),
                  ValoraEmptyState(
                    icon: Icons.error_outline_rounded,
                    title: 'Analysis Failed',
                    subtitle: provider.error,
                    actionLabel: 'Try Again',
                    onAction: () => provider.generate(inputController.text),
                  ),
                ],
              ],
            ),
          ),
        ),

        // Quick actions
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(ValoraSpacing.lg, ValoraSpacing.xl, ValoraSpacing.lg, 0),
            child: QuickActions(pdokService: pdokService, provider: provider, controller: inputController)
                .animate()
                .fadeIn(duration: 400.ms, delay: 350.ms),
          ),
        ),

        // Saved Searches section
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.only(top: ValoraSpacing.xl),
            child: SavedSearchesSection(
              controller: inputController,
              provider: provider,
            ),
          ),
        ),

        // History section
        SliverToBoxAdapter(
          child: HistorySection(
            controller: inputController,
            provider: provider,
          ),
        ),

        // Bottom padding for nav bar
        const SliverToBoxAdapter(
          child: SizedBox(height: 120.0),
        ),
      ],
    );
  }
}
