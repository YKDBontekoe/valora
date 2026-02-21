import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

/// Premium text field with refined focus animations and labels.
class ValoraTextField extends StatefulWidget {
  const ValoraTextField({
    super.key,
    this.controller,
    this.focusNode,
    this.label,
    this.hint,
    this.keyboardType,
    this.obscureText = false,
    this.prefixIcon,
    this.suffixIcon,
    this.onChanged,
    this.onSubmitted,
    this.validator,
    this.inputFormatters,
    this.textInputAction,
    this.maxLines = 1,
    this.autofocus = false,
    this.enabled = true,
    this.fillColor,
    this.autofillHints,
  });

  final TextEditingController? controller;
  final FocusNode? focusNode;
  final String? label;
  final String? hint;
  final TextInputType? keyboardType;
  final bool obscureText;
  final Widget? prefixIcon;
  final Widget? suffixIcon;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;
  final String? Function(String?)? validator;
  final List<TextInputFormatter>? inputFormatters;
  final TextInputAction? textInputAction;
  final int maxLines;
  final bool autofocus;
  final bool enabled;
  final Color? fillColor;
  final Iterable<String>? autofillHints;

  @override
  State<ValoraTextField> createState() => _ValoraTextFieldState();
}

class _ValoraTextFieldState extends State<ValoraTextField> {
  late FocusNode _focusNode;
  bool _isFocused = false;
  late bool _ownsFocusNode;

  @override
  void initState() {
    super.initState();
    _ownsFocusNode = widget.focusNode == null;
    _focusNode = widget.focusNode ?? FocusNode();
    _focusNode.addListener(_onFocusChange);
  }

  void _onFocusChange() {
    if (mounted) {
      setState(() => _isFocused = _focusNode.hasFocus);
    }
  }

  @override
  void didUpdateWidget(ValoraTextField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.focusNode != oldWidget.focusNode) {
      // Remove listener from the old node
      _focusNode.removeListener(_onFocusChange);

      // If we owned the old node, dispose of it
      if (_ownsFocusNode) {
        _focusNode.dispose();
      }

      // Update ownership flag
      _ownsFocusNode = widget.focusNode == null;

      // Assign the new node (or create a new one if null)
      _focusNode = widget.focusNode ?? FocusNode();

      // Attach listener to the new node
      _focusNode.addListener(_onFocusChange);

      // Update state
      _isFocused = _focusNode.hasFocus;
    }
  }

  @override
  void dispose() {
    // Always remove listener
    _focusNode.removeListener(_onFocusChange);

    // Only dispose if we created it
    if (_ownsFocusNode) {
      _focusNode.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final effectiveFillColor = widget.fillColor ??
        (isDark
            ? ValoraColors.surfaceVariantDark.withValues(alpha: 0.5)
            : ValoraColors.neutral50);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        // Animated label
        if (widget.label != null) ...[
          AnimatedDefaultTextStyle(
            duration: ValoraAnimations.normal,
            style: ValoraTypography.labelMedium.copyWith(
              color: _isFocused
                  ? (isDark ? ValoraColors.primaryLight : ValoraColors.primary)
                  : (isDark
                      ? ValoraColors.neutral400
                      : ValoraColors.neutral500),
              fontWeight: _isFocused ? FontWeight.w600 : FontWeight.w500,
            ),
            child: Text(widget.label!),
          ),
          const SizedBox(height: ValoraSpacing.xs),
        ],

        // Input field with animated shadow
        AnimatedContainer(
          duration: ValoraAnimations.normal,
          curve: ValoraAnimations.smooth,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
            boxShadow: _isFocused
                ? [
                    BoxShadow(
                      color: (isDark
                              ? ValoraColors.primaryLight
                              : ValoraColors.primary)
                          .withValues(alpha: 0.12),
                      blurRadius: 16,
                      offset: const Offset(0, 4),
                      spreadRadius: -2,
                    ),
                  ]
                : [],
          ),
          child: TextFormField(
            controller: widget.controller,
            focusNode: _focusNode,
            keyboardType: widget.keyboardType,
            obscureText: widget.obscureText,
            onChanged: widget.onChanged,
            onFieldSubmitted: widget.onSubmitted,
            validator: widget.validator,
            maxLines: widget.maxLines,
            autofocus: widget.autofocus,
            enabled: widget.enabled,
            inputFormatters: widget.inputFormatters,
            textInputAction: widget.textInputAction,
            autofillHints: widget.autofillHints,
            style: ValoraTypography.bodyMedium.copyWith(
              color:
                  isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
            ),
            decoration: InputDecoration(
              hintText: widget.hint,
              prefixIcon: widget.prefixIcon,
              suffixIcon: widget.suffixIcon,
              filled: true,
              fillColor: effectiveFillColor,
              contentPadding: const EdgeInsets.symmetric(
                horizontal: ValoraSpacing.lg,
                vertical: ValoraSpacing.md,
              ),
              border: OutlineInputBorder(
                borderRadius:
                    BorderRadius.circular(ValoraSpacing.radiusLg),
                borderSide: BorderSide.none,
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius:
                    BorderRadius.circular(ValoraSpacing.radiusLg),
                borderSide: BorderSide(
                  color: isDark
                      ? ValoraColors.neutral700.withValues(alpha: 0.4)
                      : ValoraColors.neutral200.withValues(alpha: 0.8),
                ),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius:
                    BorderRadius.circular(ValoraSpacing.radiusLg),
                borderSide: BorderSide(
                  color: isDark
                      ? ValoraColors.primaryLight
                      : ValoraColors.primary,
                  width: 1.5,
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}
