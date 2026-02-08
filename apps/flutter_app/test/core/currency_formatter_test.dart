import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/formatters/currency_formatter.dart';

void main() {
  test('formats euro values consistently for nl_NL locale', () {
    expect(CurrencyFormatter.formatEur(0), '€0');
    expect(CurrencyFormatter.formatEur(1000), '€1.000');
    expect(CurrencyFormatter.formatEur(1250000), '€1.250.000');
  });
}
