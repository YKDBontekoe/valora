import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../services/pdok_service.dart';
import '../../../providers/context_report_provider.dart';
import '../widgets/search_field.dart';
import '../widgets/radius_selector.dart';
import '../widgets/generate_button.dart';
import '../widgets/quick_actions.dart';
import '../widgets/history_section.dart';

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
            height: MediaQuery.of(context).padding.top + 16,
          ),
        ),

        // Hero section
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Branding row
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        gradient: const LinearGradient(
                          colors: [ValoraColors.primary, ValoraColors.primaryLight],
                          begin: Alignment.bottomLeft,
                          end: Alignment.topRight,
                        ),
                        borderRadius: BorderRadius.circular(14),
                      ),
                      child: const Icon(
                        Icons.analytics_rounded,
                        size: 24,
                        color: Colors.white,
                      ),
                    ),
                    const SizedBox(width: 14),
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
                ).animate().fadeIn(duration: 400.ms).slideX(begin: -0.05),
                const SizedBox(height: 32),

                // Search field
                SearchField(
                  controller: inputController,
                  provider: provider,
                  pdokService: pdokService,
                ).animate().fadeIn(duration: 400.ms, delay: 100.ms).slideY(begin: 0.1),

                const SizedBox(height: 16),

                // Radius selector
                const RadiusSelector()
                    .animate()
                    .fadeIn(duration: 400.ms, delay: 200.ms)
                    .slideY(begin: 0.1),

                const SizedBox(height: 20),

                // Generate button
                GenerateButton(
                  controller: inputController,
                  provider: provider,
                ).animate().fadeIn(duration: 400.ms, delay: 300.ms).slideY(begin: 0.1),

                // Error state
                if (provider.error != null) ...[
                  const SizedBox(height: 24),
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
            padding: const EdgeInsets.fromLTRB(20, 32, 20, 0),
            child: QuickActions(pdokService: pdokService, provider: provider, controller: inputController)
                .animate()
                .fadeIn(duration: 400.ms, delay: 350.ms),
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
          child: SizedBox(height: 120),
        ),
      ],
    );
  }
}
