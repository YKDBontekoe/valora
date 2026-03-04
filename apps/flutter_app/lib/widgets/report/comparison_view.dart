import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:collection/collection.dart';

import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/context_report.dart';
import '../../providers/context_report_provider.dart';
import 'score_gauge.dart';
import 'category_radar.dart';
import 'ai_insight_card.dart';

class ComparisonView extends StatelessWidget {
  const ComparisonView({super.key});

  @override
  Widget build(BuildContext context) {
    return Selector<ContextReportProvider, ({List<String> ids, List<ContextReport?> loadedReports})>(
      selector: (_, p) {
        final ids = p.comparisonIds.toList();
        final loadedReports = ids.map((id) => p.getReportById(id)).toList();
        return (ids: ids, loadedReports: loadedReports);
      },
      shouldRebuild: (previous, next) {
        final idsChanged = const ListEquality().equals(previous.ids, next.ids) == false;
        final reportsChanged = const ListEquality().equals(previous.loadedReports, next.loadedReports) == false;
        return idsChanged || reportsChanged;
      },
      builder: (context, data, child) {
        final ids = data.ids;
        final loadedReports = data.loadedReports;

        if (ids.isEmpty) {
          return const Center(child: Text('No reports to compare'));
        }

        final reports = loadedReports.whereType<ContextReport>().toList();

        // Header Row
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: List.generate(ids.length, (index) {
                  final id = ids[index];
                  final loadedReport = loadedReports[index];

                  final sep = id.lastIndexOf('|');
                  final query = sep >= 0 ? id.substring(0, sep) : id;
                  final radiusText = sep >= 0 ? id.substring(sep + 1) : null;
                  final radius = int.tryParse(radiusText ?? '') ?? 1000;

                  if (loadedReport == null) {
                     return SizedBox(
                       width: 160,
                       height: 200,
                       child: Column(
                         mainAxisAlignment: MainAxisAlignment.center,
                         children: [
                           const CircularProgressIndicator(),
                           const SizedBox(height: 16),
                           IconButton(
                             icon: const Icon(Icons.close, size: 20),
                             onPressed: () {
                               context.read<ContextReportProvider>().removeFromComparison(query, radius);
                             },
                           ),
                         ],
                       )
                     );
                  }

                  return Container(
                    width: 160,
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                    child: Column(
                      children: [
                        Text(
                          loadedReport.location.displayAddress.split(',')[0],
                          style: ValoraTypography.titleMedium,
                          textAlign: TextAlign.center,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        Text(
                          '${radius}m radius',
                          style: ValoraTypography.bodySmall,
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 8),
                        IconButton(
                          icon: const Icon(Icons.close, size: 20),
                          onPressed: () {
                             context.read<ContextReportProvider>().removeFromComparison(query, radius);
                          },
                        ),
                        const SizedBox(height: 16),
                        ScoreGauge(
                          score: loadedReport.compositeScore,
                          size: 100,
                          strokeWidth: 8,
                        ),
                        const SizedBox(height: 16),
                        AspectRatio(
                          aspectRatio: 1,
                          child: CategoryRadar(
                            categoryScores: loadedReport.categoryScores,
                            size: 100,
                          ),
                        ),
                      ],
                    ),
                  );
                }),
              ),
            ),

            const SizedBox(height: 32),

            Text('Metrics Comparison', style: ValoraTypography.titleMedium),
            const SizedBox(height: 16),
            if (reports.isNotEmpty) _buildMetricsTable(context, reports),

            const SizedBox(height: 32),

            Text('AI Insights', style: ValoraTypography.titleMedium),
            const SizedBox(height: 16),
            ...reports.map((report) => Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(report.location.displayAddress, style: ValoraTypography.labelMedium),
                  const SizedBox(height: 8),
                  AiInsightCard(report: report),
                ],
              ),
            )),
          ],
        );
      },
    );
  }

  Widget _buildMetricsTable(BuildContext context, List<ContextReport> reports) {
    final categories = <String>{};
    for (final r in reports) {
      categories.addAll(r.categoryScores.keys);
    }

    final Map<int, TableColumnWidth> colWidths = {
      0: const FixedColumnWidth(100), // Label
    };
    for (int i = 0; i < reports.length; i++) {
      colWidths[i + 1] = const FixedColumnWidth(120);
    }

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Table(
        border: TableBorder(
          horizontalInside: BorderSide(
            color: ValoraColors.neutral200,
            width: 0.5
          )
        ),
        columnWidths: colWidths,
        children: [
        // Header row
        TableRow(
          children: [
             const SizedBox(), // Label column
             ...reports.map((r) => Padding(
               padding: const EdgeInsets.all(8.0),
               child: Text(
                 r.location.displayAddress.split(',')[0],
                 style: ValoraTypography.labelSmall,
                 textAlign: TextAlign.center,
                 maxLines: 1,
                 overflow: TextOverflow.ellipsis,
               ),
             )),
          ]
        ),
          ...categories.map((category) {
             return TableRow(
               children: [
                 Padding(
                   padding: const EdgeInsets.symmetric(vertical: 12),
                   child: Text(category, style: ValoraTypography.labelSmall.copyWith(fontWeight: FontWeight.bold)),
                 ),
                 ...reports.map((report) {
                   final score = report.categoryScores[category];
                   return Padding(
                     padding: const EdgeInsets.symmetric(vertical: 12),
                     child: Center(
                       child: Text(
                         score != null ? score.toStringAsFixed(1) : '—',
                         style: ValoraTypography.bodyMedium,
                       ),
                     ),
                   );
                 }),
               ]
             );
          }),
        ],
      ),
    );
  }
}
