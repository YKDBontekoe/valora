import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/providers/theme_provider.dart';

Future<void> _waitForInit(ThemeProvider provider) async {
  int attempts = 0;
  while (!provider.isInitialized && attempts < 20) {
    await Future<void>.delayed(const Duration(milliseconds: 10));
    attempts++;
  }
}

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  test('loads persisted theme mode', () async {
    SharedPreferences.setMockInitialValues(<String, Object>{
      'theme_mode': 'dark',
    });

    final ThemeProvider provider = ThemeProvider();
    await _waitForInit(provider);

    expect(provider.themeMode, ThemeMode.dark);
    expect(provider.isDarkMode, isTrue);
  });

  test('persists theme mode updates', () async {
    final ThemeProvider provider = ThemeProvider();
    await _waitForInit(provider);

    provider.setThemeMode(ThemeMode.light);
    await Future<void>.delayed(const Duration(milliseconds: 20));

    final ThemeProvider reloadedProvider = ThemeProvider();
    await _waitForInit(reloadedProvider);

    expect(reloadedProvider.themeMode, ThemeMode.light);
  });
}
