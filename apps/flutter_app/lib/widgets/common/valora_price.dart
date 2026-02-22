import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/formatters/currency_formatter.dart';

/// A styled price display with size variants.
class ValoraPrice extends StatelessWidget {
  const ValoraPrice({
    super.key,
    required this.price,
    this.size = ValoraPriceSize.medium,
    this.color,
  });

  final double price;
  final ValoraPriceSize size;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final effectiveColor =
        color ?? (isDark ? ValoraColors.priceTagDark : ValoraColors.priceTag);

    final TextStyle style;
    switch (size) {
      case ValoraPriceSize.small:
        style = ValoraTypography.priceDisplaySmall;
        break;
      case ValoraPriceSize.medium:
        style = ValoraTypography.priceDisplay.copyWith(fontSize: 22);
        break;
      case ValoraPriceSize.large:
        style = ValoraTypography.priceDisplay;
        break;
    }

    return Text(
      CurrencyFormatter.formatEur(price),
      style: style.copyWith(color: effectiveColor),
    );
  }
}

enum ValoraPriceSize { small, medium, large }
