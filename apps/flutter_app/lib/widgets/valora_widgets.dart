import 'package:flutter/material.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';

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

    return Container(
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
    final child = Row(
      mainAxisSize: isFullWidth ? MainAxisSize.max : MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        if (isLoading)
          SizedBox(
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
        else if (icon != null)
          Icon(icon, size: ValoraSpacing.iconSizeSm + 2),
        if (icon != null || isLoading)
          const SizedBox(width: ValoraSpacing.sm),
        Text(label),
      ],
    );

    final effectiveOnPressed = isLoading ? null : onPressed;

    switch (variant) {
      case ValoraButtonVariant.primary:
        return ElevatedButton(
          onPressed: effectiveOnPressed,
          style: ElevatedButton.styleFrom(
            backgroundColor: ValoraColors.primary,
            foregroundColor: Colors.white,
          ),
          child: child,
        );
      case ValoraButtonVariant.secondary:
        return ElevatedButton(
          onPressed: effectiveOnPressed,
          child: child,
        );
      case ValoraButtonVariant.outline:
        return OutlinedButton(
          onPressed: effectiveOnPressed,
          child: child,
        );
      case ValoraButtonVariant.ghost:
        return TextButton(
          onPressed: effectiveOnPressed,
          child: child,
        );
    }
  }
}

/// Price tag component for displaying property prices.
class ValoraPrice extends StatelessWidget {
  const ValoraPrice({
    super.key,
    required this.price,
    this.size = VloraPriceSize.medium,
  });

  /// Price value in euros
  final double price;

  /// Size variant
  final VloraPriceSize size;

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
      case VloraPriceSize.small:
        style = ValoraTypography.titleMedium;
        break;
      case VloraPriceSize.medium:
        style = ValoraTypography.priceDisplay;
        break;
      case VloraPriceSize.large:
        style = ValoraTypography.headlineLarge;
        break;
    }

    return Text(
      formattedPrice,
      style: style.copyWith(color: color),
    );
  }
}

enum VloraPriceSize { small, medium, large }

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
