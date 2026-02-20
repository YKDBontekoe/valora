import 'package:flutter/foundation.dart';
import '../models/saved_property.dart';
import '../services/api_service.dart';

class SavedPropertiesProvider extends ChangeNotifier {
  ApiService _apiService;
  List<SavedProperty> _properties = [];
  bool _isLoading = false;
  String? _error;

  SavedPropertiesProvider(this._apiService);

  // Called by ChangeNotifierProxyProvider.update
  void update(ApiService apiService) {
    _apiService = apiService;
  }

  List<SavedProperty> get properties => List.unmodifiable(_properties);
  bool get isLoading => _isLoading;
  String? get error => _error;

  Future<void> fetchProperties() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _properties = await _apiService.getSavedProperties();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> saveProperty(String address, double lat, double lon, String? score) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final newProperty = await _apiService.saveProperty(
        address: address,
        latitude: lat,
        longitude: lon,
        cachedScore: score,
      );
      _properties.insert(0, newProperty);
    } catch (e) {
      _error = e.toString();
      rethrow;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> deleteProperty(String id) async {
    final index = _properties.indexWhere((p) => p.id == id);
    if (index == -1) return;

    final removed = _properties[index];
    _properties.removeAt(index);
    notifyListeners();

    try {
      await _apiService.deleteSavedProperty(id);
    } catch (e) {
      _properties.insert(index, removed);
      _error = e.toString();
      notifyListeners();
      rethrow;
    }
  }

  bool isSaved(String address) {
    return _properties.any((p) => p.address == address);
  }

  String? getSavedId(String address) {
    try {
      return _properties.firstWhere((p) => p.address == address).id;
    } catch (_) {
      return null;
    }
  }
}
