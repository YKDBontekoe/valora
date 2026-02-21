import 'package:intl/intl.dart';

abstract final class CurrencyFormatter {
  static final NumberFormat _eurNoDecimals = NumberFormat.currency(
    locale: 'nl_NL',
    symbol: '€',
    decimalDigits: 0,
  );

  static final NumberFormat _compact = NumberFormat.compactCurrency(
    locale: 'nl_NL',
    symbol: '€',
    decimalDigits: 0,
  );

  static String format(num value) {
    return _eurNoDecimals.format(value).replaceAll('\u00A0', '').trim();
  }

  // Alias for backward compatibility
  static String formatEur(num value) => format(value);

  static String formatCompact(num value) {
    return _compact.format(value).replaceAll('\u00A0', '').trim();
  }
}
