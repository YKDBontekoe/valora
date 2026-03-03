import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../core/theme/valora_spacing.dart';
import '../../../core/theme/valora_animations.dart';
import '../../../core/theme/valora_typography.dart';
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
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return CustomScrollView(
      slivers: [
        // Minimal top spacing for status bar
        SliverToBoxAdapter(
          child: SizedBox(
            height: MediaQuery.of(context).padding.top + ValoraSpacing.md,
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
                        gradient: const LinearGradient(
                          colors: [ValoraColors.primary, ValoraColors.primaryLight],
                          begin: Alignment.bottomLeft,
                          end: Alignment.topRight,
                        ),
                        borderRadius: BorderRadius.circular(ValoraSpacing.md),
                      ),
                      child: const Icon(
                        Icons.analytics_rounded,
                        size: ValoraSpacing.lg,
                        color: Colors.white,
                      ),
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
                            color: isDark
                                ? ValoraColors.neutral400
                                : ValoraColors.neutral500,
                          ),
                        ),
                      ],
                    ),
                  ],
                ).animate().fadeIn(duration: ValoraAnimations.normal).slideX(begin: -0.05),
                const SizedBox(height: ValoraSpacing.xl),

                // Search field
                SearchField(
                  controller: inputController,
                  provider: provider,
                  pdokService: pdokService,
                ).animate().fadeIn(duration: ValoraAnimations.normal, delay: ValoraAnimations.fast).slideY(begin: 0.1),

                const SizedBox(height: ValoraSpacing.md),

                // Radius selector
                const RadiusSelector()
                    .animate()
                    .fadeIn(duration: ValoraAnimations.normal, delay: ValoraAnimations.normal)
                    .slideY(begin: 0.1),

                const SizedBox(height: ValoraSpacing.lg),

                // Generate button
                GenerateButton(
                  controller: inputController,
                  provider: provider,
                ).animate().fadeIn(duration: ValoraAnimations.normal, delay: ValoraAnimations.medium).slideY(begin: 0.1),

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
                .fadeIn(duration: ValoraAnimations.normal, delay: ValoraAnimations.medium),
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
          child: SizedBox(height: ValoraSpacing.xxl * 3),
        ),
      ],
    );
  }
}
