import 'package:flutter/material.dart';
import 'package:flutter_typeahead/flutter_typeahead.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../services/pdok_service.dart';
import '../../../providers/context_report_provider.dart';

class CompactSearchField extends StatelessWidget {
  const CompactSearchField({
    super.key,
    required this.controller,
    required this.provider,
    required this.pdokService,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;
  final PdokService pdokService;

  @override
  Widget build(BuildContext context) {
    return TypeAheadField<PdokSuggestion>(
      controller: controller,
      builder: (context, controller, focusNode) {
        return ValoraSearchField(
          controller: controller,
          focusNode: focusNode,
          hintText: 'Search another address...',
          onSubmitted: (val) {
            FocusScope.of(context).unfocus();
            provider.generate(val);
          },
          onClear: provider.clear,
        );
      },
      suggestionsCallback: (pattern) async {
        if (pattern.length < 3) return [];
        return await pdokService.search(pattern);
      },
      itemBuilder: (context, suggestion) {
        return ListTile(
          leading: const Icon(Icons.location_on_outlined, size: 20),
          title: Text(suggestion.displayName,
              style: ValoraTypography.bodyMedium),
          subtitle: Text(suggestion.type,
              style: ValoraTypography.labelSmall),
        );
      },
      onSelected: (suggestion) {
        controller.text = suggestion.displayName;
        provider.generate(suggestion.displayName);
      },
    );
  }
}
