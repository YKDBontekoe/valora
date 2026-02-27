import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';

enum ValoraAvatarSize { small, medium, large }

class ValoraAvatar extends StatelessWidget {
  const ValoraAvatar({
    super.key,
    required this.initials,
    this.imageUrl,
    this.size = ValoraAvatarSize.medium,
    this.backgroundColor,
    this.textColor,
    this.showOnlineIndicator = false,
  });

  final String initials;
  final String? imageUrl;
  final ValoraAvatarSize size;
  final Color? backgroundColor;
  final Color? textColor;
  final bool showOnlineIndicator;

  double get _size {
    switch (size) {
      case ValoraAvatarSize.small:
        return ValoraSpacing.avatarSm;
      case ValoraAvatarSize.medium:
        return ValoraSpacing.avatarMd;
      case ValoraAvatarSize.large:
        return ValoraSpacing.avatarLg;
    }
  }

  TextStyle get _textStyle {
    switch (size) {
      case ValoraAvatarSize.small:
        return ValoraTypography.labelSmall.copyWith(fontWeight: FontWeight.bold);
      case ValoraAvatarSize.medium:
        return ValoraTypography.labelLarge.copyWith(fontWeight: FontWeight.bold);
      case ValoraAvatarSize.large:
        return ValoraTypography.titleLarge.copyWith(fontWeight: FontWeight.bold);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    final effectiveBackgroundColor = backgroundColor ??
        (isDark
            ? ValoraColors.primary.withValues(alpha: 0.12)
            : ValoraColors.primary.withValues(alpha: 0.12));

    final effectiveTextColor = textColor ?? ValoraColors.primary;

    return Stack(
      children: [
        Container(
          width: _size,
          height: _size,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: effectiveBackgroundColor,
            border: Border.all(color: Colors.white, width: 2),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.1),
                blurRadius: 4,
              ),
            ],
            image: imageUrl != null
                ? DecorationImage(
                    image: CachedNetworkImageProvider(imageUrl!),
                    fit: BoxFit.cover,
                  )
                : null,
          ),
          child: imageUrl == null
              ? Center(
                  child: Text(
                    initials.toUpperCase(),
                    style: _textStyle.copyWith(color: effectiveTextColor),
                  ),
                )
              : null,
        ),
        if (showOnlineIndicator)
          Positioned(
            bottom: 0,
            right: 0,
            child: Container(
              width: size == ValoraAvatarSize.small ? 8 : 12,
              height: size == ValoraAvatarSize.small ? 8 : 12,
              decoration: BoxDecoration(
                color: ValoraColors.success,
                shape: BoxShape.circle,
                border: Border.all(
                  color: theme.scaffoldBackgroundColor,
                  width: 2,
                ),
              ),
            ),
          ),
      ],
    );
  }
}
