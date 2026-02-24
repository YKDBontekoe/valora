import 'package:flutter/material.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../providers/context_report_provider.dart';

class GenerateButton extends StatelessWidget {
  const GenerateButton({
    super.key,
    required this.controller,
    required this.provider,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 56,
      child: ValueListenableBuilder<TextEditingValue>(
        valueListenable: controller,
        builder: (context, value, _) {
          return ValoraButton(
            label: 'Generate Report',
            isLoading: provider.isLoading,
            onPressed: provider.isLoading || value.text.trim().isEmpty
                ? null
                : () {
                    FocusScope.of(context).unfocus();
                    provider.generate(controller.text);
                  },
            variant: ValoraButtonVariant.primary,
            isFullWidth: true,
            size: ValoraButtonSize.large,
          );
        },
      ),
    );
  }
}
