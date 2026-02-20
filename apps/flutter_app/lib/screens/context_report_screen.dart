import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../services/api_service.dart';
import '../services/pdok_service.dart';
import '../providers/context_report_provider.dart';
import '../widgets/report/context_report_view.dart';
import '../widgets/report/context_report_skeleton.dart';
import '../widgets/report/context_report_input_form.dart';
import '../core/theme/valora_animations.dart';

class ContextReportScreen extends StatefulWidget {
  const ContextReportScreen({super.key, this.pdokService});
  final PdokService? pdokService;

  @override
  State<ContextReportScreen> createState() => _ContextReportScreenState();
}

class _ContextReportScreenState extends State<ContextReportScreen> {
  final TextEditingController _inputController = TextEditingController();
  late final PdokService _pdokService;

  @override
  @override
  void initState() {
    super.initState();
    _pdokService = widget.pdokService ?? PdokService();
  }

  @override
  void dispose() {
    _inputController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<ContextReportProvider>(
      create: (_) => ContextReportProvider(apiService: context.read<ApiService>()),
      child: Consumer<ContextReportProvider>(
        builder: (context, provider, _) {
          final report = provider.report;
          final isLoading = provider.isLoading;

          return Scaffold(
            appBar: AppBar(
              title: const Text('Property Analytics'),
              actions: [
                if (report != null)
                  IconButton(
                    tooltip: 'New Report',
                    onPressed: provider.clear,
                    icon: const Icon(Icons.refresh_rounded),
                  ),
              ],
            ),
            body: SafeArea(
              child: AnimatedSwitcher(
                duration: ValoraAnimations.normal,
                child: isLoading
                    ? const ContextReportSkeleton(key: ValueKey('loading'))
                    : report != null
                        ? ListView.builder(
                            key: const ValueKey('report-list'),
                            padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
                            itemCount: ContextReportView.childCount(report),
                            itemBuilder: (context, index) => Padding(
                              padding: const EdgeInsets.symmetric(horizontal: 16),
                              child: ContextReportView.buildChild(
                                context,
                                index,
                                report,
                              ),
                            ),
                          )
                        : ContextReportInputForm(
                            controller: _inputController,
                            provider: provider,
                            pdokService: _pdokService,
                          ),
              ),
            ),
          );
        },
      ),
    );
  }
}
