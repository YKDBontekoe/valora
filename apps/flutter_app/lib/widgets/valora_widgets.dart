import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';

/// A versatile card component with consistent styling and interactions.
///
/// Use for content containers like listings, info panels, etc.
class ValoraCard extends StatefulWidget {
  const ValoraCard({
    super.key,
    required this.child,
    this.padding,
    this.margin,
    this.onTap,
    this.elevation,
    this.borderRadius,
    this.gradient,
  });

  /// The content of the card
  final Widget child;

  /// Custom padding (defaults to ValoraSpacing.cardPadding)
  final EdgeInsetsGeometry? padding;

  /// Custom margin
  final EdgeInsetsGeometry? margin;

  /// Tap callback for interactive cards
  final VoidCallback? onTap;

  /// Custom elevation (defaults to ValoraSpacing.elevationSm)
  final double? elevation;

  /// Custom border radius (defaults to ValoraSpacing.radiusLg)
  final double? borderRadius;

  /// Optional gradient background
  final Gradient? gradient;

  @override
  State<ValoraCard> createState() => _ValoraCardState();
}

class _ValoraCardState extends State<ValoraCard> {
  bool _isHovered = false;
  bool _isPressed = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final cardRadius = widget.borderRadius ?? ValoraSpacing.radiusLg;
    final baseElevation = widget.elevation ?? ValoraSpacing.elevationSm;

    // Interactive elevation logic
    final currentElevation = _isPressed
        ? baseElevation * 0.5
        : (_isHovered ? baseElevation * 1.5 : baseElevation);

    Widget cardContent = Container(
      padding: widget.padding ?? const EdgeInsets.all(ValoraSpacing.cardPadding),
      decoration: widget.gradient != null
          ? BoxDecoration(
              gradient: widget.gradient,
              borderRadius: BorderRadius.circular(cardRadius),
            )
          : null,
      child: widget.child,
    );

    if (widget.onTap != null) {
      cardContent = Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: widget.onTap,
          onHover: (hovered) => setState(() => _isHovered = hovered),
          onTapDown: (_) => setState(() => _isPressed = true),
          onTapUp: (_) => setState(() => _isPressed = false),
          onTapCancel: () => setState(() => _isPressed = false),
          borderRadius: BorderRadius.circular(cardRadius),
          child: cardContent,
        ),
      );
    }

    return AnimatedContainer(
      duration: 200.ms,
      curve: Curves.easeOut,
      margin: widget.margin,
      decoration: BoxDecoration(
        color: widget.gradient == null
            ? (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight)
            : null,
        borderRadius: BorderRadius.circular(cardRadius),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: isDark ? 0.3 : 0.08),
            blurRadius: currentElevation * 4,
            offset: Offset(0, currentElevation * 2),
          ),
        ],
      ),
      clipBehavior: Clip.antiAlias,
      child: cardContent,
    )
    .animate(target: _isPressed ? 1 : 0)
    .scale(
      end: const Offset(0.98, 0.98),
      duration: 100.ms,
      curve: Curves.easeInOut,
    );
  }
}

/// Empty state widget for when no content is available.
class ValoraEmptyState extends StatelessWidget {
  const ValoraEmptyState({
    super.key,
    required this.icon,
    required this.title,
    this.subtitle,
    this.action,
  });

  /// Icon to display
  final IconData icon;

  /// Primary message
  final String title;

  /// Secondary message
  final String? subtitle;

  /// Optional action button
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(ValoraSpacing.xl),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(ValoraSpacing.lg),
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainerHighest,
                shape: BoxShape.circle,
              ),
              child: Icon(
                icon,
                size: ValoraSpacing.iconSizeXl,
                color: colorScheme.onSurfaceVariant,
              ),
            ).animate().fade().scale(curve: Curves.easeOutBack),
            const SizedBox(height: ValoraSpacing.lg),
            Text(
              title,
              style: textTheme.titleMedium,
              textAlign: TextAlign.center,
            ).animate().fade(delay: 100.ms).slideY(begin: 0.2, end: 0),
            if (subtitle != null) ...[
              const SizedBox(height: ValoraSpacing.sm),
              Text(
                subtitle!,
                style: textTheme.bodyMedium?.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
                textAlign: TextAlign.center,
              ).animate().fade(delay: 200.ms).slideY(begin: 0.2, end: 0),
            ],
            if (action != null) ...[
              const SizedBox(height: ValoraSpacing.lg),
              action!.animate().fade(delay: 300.ms).scale(),
            ],
          ],
        ),
      ),
    );
  }
}

/// Loading indicator with optional message.
class ValoraLoadingIndicator extends StatelessWidget {
  const ValoraLoadingIndicator({
    super.key,
    this.message,
  });

  /// Optional loading message
  final String? message;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          SizedBox(
            width: ValoraSpacing.iconSizeLg,
            height: ValoraSpacing.iconSizeLg,
            child: CircularProgressIndicator(
              strokeWidth: 3,
              valueColor: AlwaysStoppedAnimation(colorScheme.primary),
            ),
          ),
          if (message != null) ...[
            const SizedBox(height: ValoraSpacing.md),
            Text(
              message!,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: colorScheme.onSurfaceVariant,
                  ),
            ).animate().fade().shimmer(),
          ],
        ],
      ),
    );
  }
}

/// Button component with multiple variants.
enum ValoraButtonVariant { primary, secondary, outline, ghost }

class ValoraButton extends StatefulWidget {
  const ValoraButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.variant = ValoraButtonVariant.primary,
    this.icon,
    this.isLoading = false,
    this.isFullWidth = false,
  });

  /// Button label
  final String label;

  /// Press callback
  final VoidCallback? onPressed;

  /// Visual variant
  final ValoraButtonVariant variant;

  /// Optional leading icon
  final IconData? icon;

  /// Loading state
  final bool isLoading;

  /// Whether button takes full width
  final bool isFullWidth;

  @override
  State<ValoraButton> createState() => _ValoraButtonState();
}

class _ValoraButtonState extends State<ValoraButton> {
  bool _isPressed = false;

  @override
  Widget build(BuildContext context) {
    Widget child = AnimatedSwitcher(
      duration: const Duration(milliseconds: 200),
      child: widget.isLoading
          ? SizedBox(
              key: const ValueKey('loading'),
              width: ValoraSpacing.iconSizeSm,
              height: ValoraSpacing.iconSizeSm,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                valueColor: AlwaysStoppedAnimation(
                  widget.variant == ValoraButtonVariant.primary
                      ? Colors.white
                      : Theme.of(context).colorScheme.primary,
                ),
              ),
            )
          : Row(
              key: const ValueKey('content'),
              mainAxisSize: widget.isFullWidth ? MainAxisSize.max : MainAxisSize.min,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (widget.icon != null) ...[
                  Icon(widget.icon, size: ValoraSpacing.iconSizeSm + 2),
                  const SizedBox(width: ValoraSpacing.sm),
                ],
                Text(widget.label),
              ],
            ),
    );

    final effectiveOnPressed = widget.isLoading ? null : widget.onPressed;

    Widget button;
    switch (widget.variant) {
      case ValoraButtonVariant.primary:
        button = ElevatedButton(
          onPressed: effectiveOnPressed,
          style: ElevatedButton.styleFrom(
            backgroundColor: ValoraColors.primary,
            foregroundColor: Colors.white,
          ),
          child: child,
        );
        break;
      case ValoraButtonVariant.secondary:
        button = ElevatedButton(
          onPressed: effectiveOnPressed,
          child: child,
        );
        break;
      case ValoraButtonVariant.outline:
        button = OutlinedButton(
          onPressed: effectiveOnPressed,
          child: child,
        );
        break;
      case ValoraButtonVariant.ghost:
        button = TextButton(
          onPressed: effectiveOnPressed,
          child: child,
        );
        break;
    }

    if (effectiveOnPressed != null) {
      button = Listener(
        onPointerDown: (_) => setState(() => _isPressed = true),
        onPointerUp: (_) => setState(() => _isPressed = false),
        onPointerCancel: (_) => setState(() => _isPressed = false),
        child: button,
      );
    }

    return button
        .animate(target: _isPressed ? 1 : 0)
        .scale(
          end: const Offset(0.96, 0.96),
          duration: 100.ms,
          curve: Curves.easeInOut,
        );
  }
}

/// Price tag component for displaying property prices.
class ValoraPrice extends StatelessWidget {
  const ValoraPrice({
    super.key,
    required this.price,
    this.size = ValoraPriceSize.medium,
  });

  /// Price value in euros
  final double price;

  /// Size variant
  final ValoraPriceSize size;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final color = isDark ? ValoraColors.priceTagDark : ValoraColors.priceTag;

    final formattedPrice = '€ ${price.toStringAsFixed(0).replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (m) => '${m[1]}.',
        )}';

    TextStyle style;
    switch (size) {
      case ValoraPriceSize.small:
        style = ValoraTypography.titleMedium;
        break;
      case ValoraPriceSize.medium:
        style = ValoraTypography.priceDisplay;
        break;
      case ValoraPriceSize.large:
        style = ValoraTypography.headlineLarge;
        break;
    }

    return Text(
      formattedPrice,
      style: style.copyWith(color: color),
    );
  }
}

enum ValoraPriceSize { small, medium, large }

/// Badge component for status indicators.
class ValoraBadge extends StatelessWidget {
  const ValoraBadge({
    super.key,
    required this.label,
    this.color,
    this.icon,
  });

  /// Badge text
  final String label;

  /// Background color
  final Color? color;

  /// Optional icon
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final bgColor = color ?? ValoraColors.primary;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: ValoraSpacing.sm,
        vertical: ValoraSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusSm),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.1),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: ValoraSpacing.iconSizeSm, color: Colors.white),
            const SizedBox(width: ValoraSpacing.xs),
          ],
          Text(
            label,
            style: ValoraTypography.labelSmall.copyWith(
              color: Colors.white,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    ).animate().fadeIn().scale(curve: Curves.easeOutBack);
  }
}

/// A shimmer effect widget for loading states.
class ValoraShimmer extends StatelessWidget {
  const ValoraShimmer({
    super.key,
    required this.width,
    required this.height,
    this.borderRadius,
  });

  final double width;
  final double height;
  final double? borderRadius;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final baseColor =
        isDark ? ValoraColors.surfaceVariantDark : ValoraColors.neutral100;

    // flutter_animate's shimmer is easier to use
    return Container(
      width: width,
      height: height,
      decoration: BoxDecoration(
        color: baseColor,
        borderRadius: BorderRadius.circular(
          borderRadius ?? ValoraSpacing.radiusMd,
        ),
      ),
    ).animate(onPlay: (controller) => controller.repeat())
     .shimmer(
       duration: 1500.ms,
       color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral50,
     );
  }
}

/// A premium text field with consistent styling.
class ValoraTextField extends StatefulWidget {
  const ValoraTextField({
    super.key,
    this.controller,
    required this.label,
    this.hint,
    this.keyboardType,
    this.prefixIcon,
    this.prefixText,
    this.onChanged,
    this.validator,
    this.obscureText = false,
    this.textInputAction,
    this.onFieldSubmitted,
    this.autofillHints,
  });

  final TextEditingController? controller;
  final String label;
  final String? hint;
  final TextInputType? keyboardType;
  final IconData? prefixIcon;
  final String? prefixText;
  final ValueChanged<String>? onChanged;
  final FormFieldValidator<String>? validator;
  final bool obscureText;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onFieldSubmitted;
  final Iterable<String>? autofillHints;

  @override
  State<ValoraTextField> createState() => _ValoraTextFieldState();
}

class _ValoraTextFieldState extends State<ValoraTextField> {
  late FocusNode _focusNode;
  bool _isFocused = false;

  @override
  void initState() {
    super.initState();
    _focusNode = FocusNode();
    _focusNode.addListener(() {
      setState(() {
        _isFocused = _focusNode.hasFocus;
      });
    });
  }

  @override
  void dispose() {
    _focusNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          widget.label,
          style: ValoraTypography.labelMedium.copyWith(
            color: _isFocused
                ? Theme.of(context).colorScheme.primary
                : Theme.of(context).colorScheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: ValoraSpacing.xs),
        TextFormField(
          controller: widget.controller,
          focusNode: _focusNode,
          keyboardType: widget.keyboardType,
          onChanged: widget.onChanged,
          validator: widget.validator,
          obscureText: widget.obscureText,
          textInputAction: widget.textInputAction,
          onFieldSubmitted: widget.onFieldSubmitted,
          autofillHints: widget.autofillHints,
          style: ValoraTypography.bodyMedium,
          decoration: InputDecoration(
            hintText: widget.hint,
            prefixIcon: widget.prefixIcon != null
                ? Icon(widget.prefixIcon, size: ValoraSpacing.iconSizeSm)
                : null,
            prefixText: widget.prefixText,
            prefixStyle: ValoraTypography.bodyMedium,
          ),
        ),
      ],
    );
  }
}

/// A premium choice chip with selection animations.
class ValoraChip extends StatelessWidget {
  const ValoraChip({
    super.key,
    required this.label,
    required this.isSelected,
    required this.onSelected,
  });

  final String label;
  final bool isSelected;
  final ValueChanged<bool> onSelected;

  @override
  Widget build(BuildContext context) {
    return AnimatedContainer(
      duration: const Duration(milliseconds: 200),
      child: FilterChip(
        label: Text(label),
        selected: isSelected,
        onSelected: onSelected,
        checkmarkColor: isSelected ? Colors.white : null,
        labelStyle: ValoraTypography.labelMedium.copyWith(
          color: isSelected
              ? Colors.white
              : Theme.of(context).colorScheme.onSurface,
        ),
        backgroundColor: Theme.of(context).colorScheme.surface,
        selectedColor: ValoraColors.primary,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          side: BorderSide(
            color: isSelected
                ? Colors.transparent
                : Theme.of(context).colorScheme.outline,
          ),
        ),
        showCheckmark: false,
        padding: const EdgeInsets.symmetric(
          horizontal: ValoraSpacing.sm,
          vertical: ValoraSpacing.xs,
        ),
      ),
    ).animate(target: isSelected ? 1 : 0)
    .scale(end: const Offset(1.05, 1.05), duration: 200.ms, curve: Curves.easeOutBack)
    .then()
    .scale(end: const Offset(1.0, 1.0), duration: 200.ms);
  }
}

/// A styled dialog container using `ValoraCard` styling.
class ValoraDialog extends StatelessWidget {
  const ValoraDialog({
    super.key,
    required this.title,
    required this.child,
    required this.actions,
  });

  final String title;
  final Widget child;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: Colors.transparent,
      elevation: 0,
      insetPadding: const EdgeInsets.all(ValoraSpacing.md),
      child: ValoraCard(
        padding: const EdgeInsets.all(ValoraSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              title,
              style: ValoraTypography.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: ValoraSpacing.lg),
            Flexible(
              child: SingleChildScrollView(
                child: child,
              ),
            ),
            const SizedBox(height: ValoraSpacing.xl),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: actions.map((action) {
                return Padding(
                  padding: const EdgeInsets.only(left: ValoraSpacing.sm),
                  child: action,
                );
              }).toList(),
            ),
          ],
        ),
      ),
    ).animate().fade().scale(curve: Curves.easeOutBack);
  }
}
