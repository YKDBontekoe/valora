import 'package:intl/intl.dart';

abstract final class CurrencyFormatter {
  static final NumberFormat _eurNoDecimals = NumberFormat.currency(
    locale: 'nl_NL',
    symbol: '€',
    decimalDigits: 0,
  );

  static String formatEur(num value) {
    return _eurNoDecimals.format(value).replaceAll('\u00A0', '').trim();
  }
}
