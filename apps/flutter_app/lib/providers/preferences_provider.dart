import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

class PreferencesProvider extends ChangeNotifier {
  static const String _notificationsEnabledKey = 'notifications_enabled';
  static const String _defaultCityKey = 'default_city';
  static const String _defaultMinPriceKey = 'default_min_price';
  static const String _defaultMaxPriceKey = 'default_max_price';
  static const String _defaultMinBedroomsKey = 'default_min_bedrooms';

  bool _notificationsEnabled = true;
  String? _defaultCity;
  double? _defaultMinPrice;
  double? _defaultMaxPrice;
  int? _defaultMinBedrooms;
  bool _isInitialized = false;

  bool get notificationsEnabled => _notificationsEnabled;
  String? get defaultCity => _defaultCity;
  double? get defaultMinPrice => _defaultMinPrice;
  double? get defaultMaxPrice => _defaultMaxPrice;
  int? get defaultMinBedrooms => _defaultMinBedrooms;
  bool get isInitialized => _isInitialized;

  PreferencesProvider() {
    _loadPreferences();
  }

  Future<void> _loadPreferences() async {
    final prefs = await SharedPreferences.getInstance();
    _notificationsEnabled = prefs.getBool(_notificationsEnabledKey) ?? true;
    _defaultCity = prefs.getString(_defaultCityKey);
    _defaultMinPrice = prefs.getDouble(_defaultMinPriceKey);
    _defaultMaxPrice = prefs.getDouble(_defaultMaxPriceKey);
    _defaultMinBedrooms = prefs.getInt(_defaultMinBedroomsKey);
    _isInitialized = true;
    notifyListeners();
  }

  Future<void> setNotificationsEnabled(bool enabled) async {
    _notificationsEnabled = enabled;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(_notificationsEnabledKey, enabled);
  }

  Future<void> setDefaultCity(String? city) async {
    _defaultCity = city;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    if (city == null) {
      await prefs.remove(_defaultCityKey);
    } else {
      await prefs.setString(_defaultCityKey, city);
    }
  }

  Future<void> setDefaultMinPrice(double? price) async {
    _defaultMinPrice = price;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    if (price == null) {
      await prefs.remove(_defaultMinPriceKey);
    } else {
      await prefs.setDouble(_defaultMinPriceKey, price);
    }
  }

  Future<void> setDefaultMaxPrice(double? price) async {
    _defaultMaxPrice = price;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    if (price == null) {
      await prefs.remove(_defaultMaxPriceKey);
    } else {
      await prefs.setDouble(_defaultMaxPriceKey, price);
    }
  }

  Future<void> setDefaultMinBedrooms(int? bedrooms) async {
    _defaultMinBedrooms = bedrooms;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    if (bedrooms == null) {
      await prefs.remove(_defaultMinBedroomsKey);
    } else {
      await prefs.setInt(_defaultMinBedroomsKey, bedrooms);
    }
  }

  Future<void> clearAll() async {
    _notificationsEnabled = true;
    _defaultCity = null;
    _defaultMinPrice = null;
    _defaultMaxPrice = null;
    _defaultMinBedrooms = null;
    notifyListeners();

    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_notificationsEnabledKey);
    await prefs.remove(_defaultCityKey);
    await prefs.remove(_defaultMinPriceKey);
    await prefs.remove(_defaultMaxPriceKey);
    await prefs.remove(_defaultMinBedroomsKey);
  }
}
