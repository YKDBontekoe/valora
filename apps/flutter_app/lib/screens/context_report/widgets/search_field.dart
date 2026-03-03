import 'package:flutter/material.dart';
import 'package:flutter_typeahead/flutter_typeahead.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../core/theme/valora_spacing.dart';
import '../../../core/theme/valora_animations.dart';
import '../../../services/pdok_service.dart';
import '../../../providers/context_report_provider.dart';

class SearchField extends StatelessWidget {
  const SearchField({
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
        return MouseRegion(
          cursor: SystemMouseCursors.text,
          child: AnimatedBuilder(
            animation: focusNode,
            builder: (context, child) {
              return AnimatedContainer(
                duration: ValoraAnimations.fast,
                curve: Curves.easeOut,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                  boxShadow: focusNode.hasFocus
                      ? [
                          BoxShadow(
                            color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.1),
                            blurRadius: ValoraSpacing.sm,
                            offset: const Offset(0, 2),
                          )
                        ]
                      : const [],
                ),
                child: ValoraTextField(
                  controller: controller,
                  focusNode: focusNode,
                  hint: 'Search city, zip, or address...',
                  label: 'Address',
                  prefixIcon: const Icon(Icons.search_rounded),
                  suffixIcon: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (controller.text.isNotEmpty)
                        IconButton(
                          icon: const Icon(Icons.clear_rounded, size: ValoraSpacing.iconSizeSm),
                          onPressed: () => controller.clear(),
                        ),
                    ],
                  ),
                  textInputAction: TextInputAction.search,
                  onSubmitted: (val) => _handleSubmit(context, val),
                ),
              );
            },
          ),
        );
      },
      suggestionsCallback: (pattern) async {
        if (pattern.length < 3) return [];
        return await pdokService.search(pattern);
      },
      itemBuilder: (context, suggestion) {
        return ListTile(
          leading: const Icon(Icons.location_on_outlined, size: ValoraSpacing.iconSizeSm),
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

  void _handleSubmit(BuildContext context, String value) {
    FocusScope.of(context).unfocus();
    provider.generate(value);
  }
}
