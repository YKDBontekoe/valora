import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

class ValoraSearchField extends StatefulWidget {
  const ValoraSearchField({
    super.key,
    required this.controller,
    this.focusNode,
    this.hintText = 'Search...',
    this.onChanged,
    this.onSubmitted,
    this.onClear,
    this.autoFocus = false,
  });

  final TextEditingController controller;
  final FocusNode? focusNode;
  final String hintText;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;
  final VoidCallback? onClear;
  final bool autoFocus;

  @override
  State<ValoraSearchField> createState() => _ValoraSearchFieldState();
}

class _ValoraSearchFieldState extends State<ValoraSearchField> {
  late FocusNode _focusNode;
  bool _isFocused = false;
  late bool _ownsFocusNode;

  @override
  void initState() {
    super.initState();
    _ownsFocusNode = widget.focusNode == null;
    _focusNode = widget.focusNode ?? FocusNode();
    _focusNode.addListener(_onFocusChange);
    widget.controller.addListener(_onTextChange);
    // Initial check
    _isFocused = _focusNode.hasFocus;
  }

  void _onFocusChange() {
    if (mounted) {
      setState(() => _isFocused = _focusNode.hasFocus);
    }
  }

  void _onTextChange() {
    if (mounted) {
      setState(() {});
    }
  }

  @override
  void didUpdateWidget(ValoraSearchField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.focusNode != oldWidget.focusNode) {
      _focusNode.removeListener(_onFocusChange);
      if (_ownsFocusNode) {
        _focusNode.dispose();
      }
      _ownsFocusNode = widget.focusNode == null;
      _focusNode = widget.focusNode ?? FocusNode();
      _focusNode.addListener(_onFocusChange);
      _isFocused = _focusNode.hasFocus;
    }
    if (widget.controller != oldWidget.controller) {
      oldWidget.controller.removeListener(_onTextChange);
      widget.controller.addListener(_onTextChange);
    }
  }

  @override
  void dispose() {
    _focusNode.removeListener(_onFocusChange);
    widget.controller.removeListener(_onTextChange);
    if (_ownsFocusNode) {
      _focusNode.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    final backgroundColor = isDark
        ? ValoraColors.surfaceDark
        : ValoraColors.neutral50;

    final borderColor = _isFocused
        ? (isDark ? ValoraColors.primaryLight : ValoraColors.primary)
        : (isDark
            ? ValoraColors.neutral700.withValues(alpha: 0.4)
            : ValoraColors.neutral200);

    final iconColor = _isFocused
        ? (isDark ? ValoraColors.primaryLight : ValoraColors.primary)
        : (isDark ? ValoraColors.neutral500 : ValoraColors.neutral400);

    return AnimatedContainer(
      duration: ValoraAnimations.fast,
      height: 48,
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
        border: Border.all(color: borderColor, width: _isFocused ? 1.5 : 1.0),
        boxShadow: _isFocused
            ? [
                BoxShadow(
                  color: (isDark
                          ? ValoraColors.primaryLight
                          : ValoraColors.primary)
                      .withValues(alpha: 0.1),
                  blurRadius: 8,
                  offset: const Offset(0, 2),
                ),
              ]
            : [],
      ),
      child: TextField(
        controller: widget.controller,
        focusNode: _focusNode,
        textInputAction: TextInputAction.search,
        onChanged: widget.onChanged,
        onSubmitted: widget.onSubmitted,
        autofocus: widget.autoFocus,
        style: ValoraTypography.bodyMedium.copyWith(
          color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
        ),
        cursorColor: isDark ? ValoraColors.primaryLight : ValoraColors.primary,
        decoration: InputDecoration(
          hintText: widget.hintText,
          hintStyle: ValoraTypography.bodyMedium.copyWith(
            color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
          ),
          prefixIcon: Icon(
            Icons.search_rounded,
            size: 20,
            color: iconColor,
          ),
          suffixIcon: widget.controller.text.isNotEmpty
              ? IconButton(
                  icon: Icon(
                    Icons.close_rounded,
                    size: 18,
                    color: isDark
                        ? ValoraColors.neutral400
                        : ValoraColors.neutral500,
                  ),
                  onPressed: () {
                    widget.controller.clear();
                    widget.onClear?.call();
                  },
                )
              : null,
          border: InputBorder.none,
          contentPadding: const EdgeInsets.symmetric(vertical: 13),
        ),
      ),
    );
  }
}
