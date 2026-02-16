import 'package:flutter/material.dart';
import 'package:flutter_typeahead/flutter_typeahead.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
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
    final isDark = Theme.of(context).brightness == Brightness.dark;

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
        decorationBuilder: (context, child) {
          return Material(
            type: MaterialType.card,
            elevation: ValoraSpacing.elevationMd,
            color: isDark ? ValoraColors.surfaceVariantDark : ValoraColors.surfaceLight,
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
            clipBehavior: Clip.antiAlias,
            child: child,
          );
        },
        builder: (context, controller, focusNode) {
          return ValoraTextField(
            controller: controller,
            focusNode: focusNode,
            label: null, // Removed empty label string
            hint: 'City, address, or zip code...',
            prefixIcon: Icon(
              Icons.search_rounded,
              color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
            ),
            textInputAction: TextInputAction.search,
            onSubmitted: (_) => onSubmitted(),
          );
        },
        itemBuilder: (context, suggestion) {
          return ListTile(
            leading: Icon(
              Icons.location_on_outlined,
              color: isDark ? ValoraColors.primaryLight : ValoraColors.primary,
              size: ValoraSpacing.iconSizeSm,
            ),
            title: Text(
              suggestion.displayName,
              style: ValoraTypography.bodyMedium.copyWith(
                color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
              ),
            ),
            subtitle: Text(
              suggestion.type,
              style: ValoraTypography.labelSmall.copyWith(
                color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
              ),
            ),
            contentPadding: const EdgeInsets.symmetric(
              horizontal: ValoraSpacing.md,
              vertical: ValoraSpacing.xs,
            ),
          );
        },
        onSelected: onSuggestionSelected,
        emptyBuilder: (context) => Padding(
          padding: const EdgeInsets.all(ValoraSpacing.lg),
          child: Text(
            'No address found. Try entering a street and number.',
            style: ValoraTypography.bodyMedium.copyWith(
              color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
            ),
            textAlign: TextAlign.center,
          ),
        ),
      ),
    );
  }
}
