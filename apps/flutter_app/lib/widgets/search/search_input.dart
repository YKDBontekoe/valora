import 'package:flutter/material.dart';
import 'package:flutter_typeahead/flutter_typeahead.dart';
import '../../core/theme/valora_spacing.dart';
import '../../services/pdok_service.dart';
import '../valora_widgets.dart';

class SearchInput extends StatelessWidget {
  final TextEditingController controller;
  final PdokService pdokService;
  final Function(PdokSuggestion) onSuggestionSelected;
  final VoidCallback onSubmitted;
  final Duration debounceDuration;

  const SearchInput({
    super.key,
    required this.controller,
    required this.pdokService,
    required this.onSuggestionSelected,
    required this.onSubmitted,
    this.debounceDuration = const Duration(milliseconds: 400),
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        ValoraSpacing.lg,
        0,
        ValoraSpacing.lg,
        ValoraSpacing.md,
      ),
      child: TypeAheadField<PdokSuggestion>(
        controller: controller,
        debounceDuration: debounceDuration,
        suggestionsCallback: (pattern) async {
          return await pdokService.search(pattern);
        },
        builder: (context, controller, focusNode) {
          return ValoraTextField(
            controller: controller,
            focusNode: focusNode,
            label: '',
            hint: 'City, address, or zip code...',
            prefixIcon: const Icon(Icons.search_rounded),
            textInputAction: TextInputAction.search,
            onSubmitted: (_) => onSubmitted(),
          );
        },
        itemBuilder: (context, suggestion) {
          return ListTile(
            leading: const Icon(Icons.location_on_outlined),
            title: Text(suggestion.displayName),
            subtitle: Text(suggestion.type),
          );
        },
        onSelected: onSuggestionSelected,
        emptyBuilder: (context) => const Padding(
          padding: EdgeInsets.all(16.0),
          child: Text('No address found. Try entering a street and number.'),
        ),
      ),
    );
  }
}
