import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';

/// A premium text field with consistent styling.
class ValoraTextField extends StatefulWidget {
  const ValoraTextField({
    super.key,
    this.controller,
    this.focusNode,
    required this.label,
    this.hint,
    this.keyboardType,
    this.prefixIcon,
    this.suffixIcon,
    this.prefixText,
    this.onChanged,
    this.validator,
    this.obscureText = false,
    this.textInputAction,
    this.onSubmitted,
    this.autofillHints,
    this.inputFormatters,
  });

  final TextEditingController? controller;
  final FocusNode? focusNode;
  final String label;
  final String? hint;
  final TextInputType? keyboardType;
  final IconData? prefixIcon;
  final Widget? suffixIcon;
  final String? prefixText;
  final ValueChanged<String>? onChanged;
  final FormFieldValidator<String>? validator;
  final bool obscureText;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onSubmitted;
  final Iterable<String>? autofillHints;
  final List<TextInputFormatter>? inputFormatters;

  @override
  State<ValoraTextField> createState() => _ValoraTextFieldState();
}

class _ValoraTextFieldState extends State<ValoraTextField> {
  late FocusNode _focusNode;
  bool _isFocused = false;

  @override
  void initState() {
    super.initState();
    _focusNode = widget.focusNode ?? FocusNode();
    _focusNode.addListener(_handleFocusChange);
    _isFocused = _focusNode.hasFocus;
  }

  void _handleFocusChange() {
    setState(() {
      _isFocused = _focusNode.hasFocus;
    });
  }

  @override
  void didUpdateWidget(ValoraTextField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.focusNode != oldWidget.focusNode) {
      if (oldWidget.focusNode == null) {
        // We were using the internal node, dispose it
        _focusNode.removeListener(_handleFocusChange);
        _focusNode.dispose();
      } else {
        // Remove listener from the old external node
        oldWidget.focusNode!.removeListener(_handleFocusChange);
      }
      
      _focusNode = widget.focusNode ?? FocusNode();
      _focusNode.addListener(_handleFocusChange);
      _isFocused = _focusNode.hasFocus;
    }
  }

  @override
  void dispose() {
    _focusNode.removeListener(_handleFocusChange);
    if (widget.focusNode == null) {
      _focusNode.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (widget.label.isNotEmpty) ...[
          Text(
            widget.label,
            style: ValoraTypography.labelMedium.copyWith(
              color: _isFocused
                  ? Theme.of(context).colorScheme.primary
                  : Theme.of(context).colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(height: ValoraSpacing.xs),
        ],
        TextFormField(
          controller: widget.controller,
          focusNode: _focusNode,
          keyboardType: widget.keyboardType,
          onChanged: widget.onChanged,
          validator: widget.validator,
          obscureText: widget.obscureText,
          textInputAction: widget.textInputAction,
          onFieldSubmitted: widget.onSubmitted,
          autofillHints: widget.autofillHints,
          inputFormatters: widget.inputFormatters,
          style: ValoraTypography.bodyMedium,
          decoration: InputDecoration(
            hintText: widget.hint,
            prefixIcon: widget.prefixIcon != null
                ? Icon(widget.prefixIcon, size: ValoraSpacing.iconSizeSm)
                : null,
            prefixText: widget.prefixText,
            prefixStyle: ValoraTypography.bodyMedium,
            suffixIcon: widget.suffixIcon,
          ),
        ),
      ],
    );
  }
}
