import 'package:flutter/material.dart';
import '../../../providers/context_report_provider.dart';
import '../../../services/pdok_service.dart';
import '../../../widgets/report/context_report_view.dart';
import '../../../widgets/report/context_report_skeleton.dart';
import '../widgets/compact_search_field.dart';
import '../widgets/compare_button.dart';
import '../widgets/save_search_button.dart';

class ReportLayout extends StatelessWidget {
  const ReportLayout({
    super.key,
    required this.inputController,
    required this.provider,
    required this.pdokService,
    required this.isLoading,
  });

  final TextEditingController inputController;
  final ContextReportProvider provider;
  final PdokService pdokService;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return CustomScrollView(
      slivers: [
        // Persistent search bar at the top
        SliverAppBar(
          pinned: true,
          floating: true,
          snap: true,
          automaticallyImplyLeading: false,
          backgroundColor: colorScheme.surface.withValues(alpha: 0.97),
          surfaceTintColor: Colors.transparent,
          toolbarHeight: 76,
          title: Row(
            children: [
              Expanded(
                child: CompactSearchField(
                  controller: inputController,
                  provider: provider,
                  pdokService: pdokService,
                ),
              ),
              if (provider.report != null) ...[
                const SizedBox(width: 8),
                // Save Search Button
                SaveSearchButton(provider: provider),
                const SizedBox(width: 8),
                // Compare toggle
                CompareButton(provider: provider),
              ],
            ],
          ),
        ),

        // Content
        if (isLoading)
          const SliverFillRemaining(
            child: ContextReportSkeleton(),
          )
        else if (provider.report != null)
          SliverPadding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
            sliver: SliverList(
              delegate: SliverChildBuilderDelegate(
                (context, index) => Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 4),
                  child: ContextReportView.buildChild(
                    context,
                    index,
                    provider.report!,
                  ),
                ),
                childCount:
                    ContextReportView.childCount(provider.report!),
              ),
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
