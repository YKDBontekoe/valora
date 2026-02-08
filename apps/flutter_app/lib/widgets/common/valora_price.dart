import 'package:flutter/material.dart';
import '../../core/formatters/currency_formatter.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';

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

    final formattedPrice = CurrencyFormatter.formatEur(price);

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

    return Text(formattedPrice, style: style.copyWith(color: color));
  }
}

enum ValoraPriceSize { small, medium, large }
