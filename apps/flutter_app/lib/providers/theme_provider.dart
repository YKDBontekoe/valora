import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

class ThemeProvider extends ChangeNotifier {
  static const String _themeModeKey = 'theme_mode';

  ThemeMode _themeMode = ThemeMode.system;
  bool _isInitialized = false;

  ThemeMode get themeMode => _themeMode;
  bool get isInitialized => _isInitialized;

  bool get isDarkMode {
    return _themeMode == ThemeMode.dark;
  }

  ThemeProvider() {
    _loadThemeMode();
  }

  Future<void> _loadThemeMode() async {
    final SharedPreferences preferences = await SharedPreferences.getInstance();
    final String? savedMode = preferences.getString(_themeModeKey);
    _themeMode = _fromStoredValue(savedMode);
    _isInitialized = true;
    notifyListeners();
  }

  void toggleTheme() {
    final ThemeMode nextMode = _themeMode == ThemeMode.dark
        ? ThemeMode.light
        : ThemeMode.dark;
    setThemeMode(nextMode);
  }

  void setThemeMode(ThemeMode mode) {
    _themeMode = mode;
    notifyListeners();
    _persistThemeMode(mode);
  }

  Future<void> _persistThemeMode(ThemeMode mode) async {
    final SharedPreferences preferences = await SharedPreferences.getInstance();
    await preferences.setString(_themeModeKey, _toStoredValue(mode));
  }

  static ThemeMode _fromStoredValue(String? value) {
    switch (value) {
      case 'light':
        return ThemeMode.light;
      case 'dark':
        return ThemeMode.dark;
      case 'system':
      default:
        return ThemeMode.system;
    }
  }

  static String _toStoredValue(ThemeMode mode) {
    switch (mode) {
      case ThemeMode.light:
        return 'light';
      case ThemeMode.dark:
        return 'dark';
      case ThemeMode.system:
        return 'system';
    }
  }
}
