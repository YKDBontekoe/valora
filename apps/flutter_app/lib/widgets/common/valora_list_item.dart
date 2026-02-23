import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import 'valora_card.dart';

/// A generic, premium list item component for lists and menus.
class ValoraListItem extends StatelessWidget {
  const ValoraListItem({
    super.key,
    required this.title,
    this.subtitle,
    this.subtitleWidget,
    this.leading,
    this.trailing,
    this.onTap,
    this.showChevron = true,
    this.padding,
    this.backgroundColor,
  });

  /// The primary text of the list item.
  final String title;

  /// Optional secondary text below the title.
  final String? subtitle;

  /// Optional widget to display below the title. Overrides [subtitle].
  final Widget? subtitleWidget;

  /// Optional widget to display before the title (e.g., icon, avatar).
  final Widget? leading;

  /// Optional widget to display after the title (e.g., status badge, action button).
  final Widget? trailing;

  /// Callback when the item is tapped.
  final VoidCallback? onTap;

  /// Whether to show a chevron icon if [onTap] is provided and [trailing] is null.
  final bool showChevron;

  /// Custom padding for the content.
  final EdgeInsetsGeometry? padding;

  /// Custom background color.
  final Color? backgroundColor;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;
    final isInteractive = onTap != null;

    final textColor = colorScheme.onSurface;
    final subtitleColor = colorScheme.onSurfaceVariant;

    // Hover/Press background color logic is handled by ValoraCard

    return ValoraCard(
      padding: EdgeInsets.zero,
      onTap: onTap,
      backgroundColor: backgroundColor,
      child: Padding(
        padding: padding ?? const EdgeInsets.all(ValoraSpacing.md),
        child: Row(
          children: [
            // Leading
            if (leading != null) ...[
              leading!,
              const SizedBox(width: ValoraSpacing.md),
            ],

            // Text Content
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    title,
                    style: ValoraTypography.titleSmall.copyWith(
                      color: textColor,
                      fontWeight: FontWeight.w600,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (subtitleWidget != null) ...[
                    const SizedBox(height: 4),
                    subtitleWidget!,
                  ] else if (subtitle != null) ...[
                    const SizedBox(height: 4),
                    Text(
                      subtitle!,
                      style: ValoraTypography.bodySmall.copyWith(
                        color: subtitleColor,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ],
              ),
            ),

            // Trailing
            if (trailing != null) ...[
              const SizedBox(width: ValoraSpacing.md),
              trailing!,
            ] else if (showChevron && isInteractive) ...[
              const SizedBox(width: ValoraSpacing.sm),
              Icon(
                Icons.chevron_right_rounded,
                color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                size: 20,
              ),
            ],
          ],
        ),
      ),
    );
  }
}
