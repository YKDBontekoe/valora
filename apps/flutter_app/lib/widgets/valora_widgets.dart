import 'package:flutter/material.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';

/// A wrapper that adds a subtle scale animation on press.
class _ValoraScaleWrapper extends StatefulWidget {
  const _ValoraScaleWrapper({
    required this.child,
    this.enabled = true,
  });

  final Widget child;
  final bool enabled;

  @override
  State<_ValoraScaleWrapper> createState() => _ValoraScaleWrapperState();
}

class _ValoraScaleWrapperState extends State<_ValoraScaleWrapper>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _scaleAnimation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 100),
    );
    _scaleAnimation = Tween<double>(begin: 1.0, end: 0.98).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (!widget.enabled) return widget.child;

    return Listener(
      onPointerDown: (_) => _controller.forward(),
      onPointerUp: (_) => _controller.reverse(),
      onPointerCancel: (_) => _controller.reverse(),
      child: ScaleTransition(
        scale: _scaleAnimation,
        child: widget.child,
      ),
    );
  }
}

/// A versatile card component with consistent styling.
///
/// Use for content containers like listings, info panels, etc.
class ValoraCard extends StatelessWidget {
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
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final cardRadius = borderRadius ?? ValoraSpacing.radiusLg;

    Widget cardContent = Container(
      padding: padding ?? const EdgeInsets.all(ValoraSpacing.cardPadding),
      decoration: gradient != null
          ? BoxDecoration(
              gradient: gradient,
              borderRadius: BorderRadius.circular(cardRadius),
            )
          : null,
      child: child,
    );

    if (onTap != null) {
      cardContent = Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(cardRadius),
          child: cardContent,
        ),
      );
    }

    final card = Container(
      margin: margin,
      decoration: BoxDecoration(
        color: gradient == null
            ? (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight)
            : null,
        borderRadius: BorderRadius.circular(cardRadius),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: isDark ? 0.3 : 0.08),
            blurRadius: (elevation ?? ValoraSpacing.elevationSm) * 4,
            offset: Offset(0, (elevation ?? ValoraSpacing.elevationSm) * 2),
          ),
        ],
      ),
      clipBehavior: Clip.antiAlias,
      child: cardContent,
    );

    if (onTap != null) {
      return _ValoraScaleWrapper(child: card);
    }

    return card;
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
            ),
            const SizedBox(height: ValoraSpacing.lg),
            Text(
              title,
              style: textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            if (subtitle != null) ...[
              const SizedBox(height: ValoraSpacing.sm),
              Text(
                subtitle!,
                style: textTheme.bodyMedium?.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
                textAlign: TextAlign.center,
              ),
            ],
            if (action != null) ...[
              const SizedBox(height: ValoraSpacing.lg),
              action!,
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
            ),
          ],
        ],
      ),
    );
  }
}

/// Button component with multiple variants.
enum ValoraButtonVariant { primary, secondary, outline, ghost }

class ValoraButton extends StatelessWidget {
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
  Widget build(BuildContext context) {
    Widget child = AnimatedSwitcher(
      duration: const Duration(milliseconds: 200),
      child: isLoading
          ? SizedBox(
              key: const ValueKey('loading'),
              width: ValoraSpacing.iconSizeSm,
              height: ValoraSpacing.iconSizeSm,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                valueColor: AlwaysStoppedAnimation(
                  variant == ValoraButtonVariant.primary
                      ? Colors.white
                      : Theme.of(context).colorScheme.primary,
                ),
              ),
            )
          : Row(
              key: const ValueKey('content'),
              mainAxisSize: isFullWidth ? MainAxisSize.max : MainAxisSize.min,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (icon != null) ...[
                  Icon(icon, size: ValoraSpacing.iconSizeSm + 2),
                  const SizedBox(width: ValoraSpacing.sm),
                ],
                Text(label),
              ],
            ),
    );

    final effectiveOnPressed = isLoading ? null : onPressed;

    Widget button;
    switch (variant) {
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

    return _ValoraScaleWrapper(
      enabled: onPressed != null && !isLoading,
      child: button,
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
    );
  }
}

/// A shimmer effect widget for loading states.
class ValoraShimmer extends StatefulWidget {
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
  State<ValoraShimmer> createState() => _ValoraShimmerState();
}

class _ValoraShimmerState extends State<ValoraShimmer>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _animation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1500),
    )..repeat();

    _animation = Tween<double>(begin: -1.0, end: 2.0).animate(
      CurvedAnimation(parent: _controller, curve: Curves.linear),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final baseColor =
        isDark ? ValoraColors.surfaceVariantDark : ValoraColors.neutral100;
    final highlightColor =
        isDark ? ValoraColors.neutral700 : ValoraColors.neutral50;

    return AnimatedBuilder(
      animation: _animation,
      builder: (context, child) {
        return Container(
          width: widget.width,
          height: widget.height,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(
              widget.borderRadius ?? ValoraSpacing.radiusMd,
            ),
            gradient: LinearGradient(
              begin: Alignment.centerLeft,
              end: Alignment.centerRight,
              colors: [baseColor, highlightColor, baseColor],
              stops: [
                0.0,
                0.5,
                1.0,
              ],
              transform: _SlidingGradientTransform(_animation.value),
            ),
          ),
        );
      },
    );
  }
}

class _SlidingGradientTransform extends GradientTransform {
  const _SlidingGradientTransform(this.slidePercent);

  final double slidePercent;

  @override
  Matrix4? transform(Rect bounds, {TextDirection? textDirection}) {
    return Matrix4.translationValues(bounds.width * slidePercent, 0.0, 0.0);
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
    );
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
    );
  }
}
