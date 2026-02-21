import 'package:shared_preferences/shared_preferences.dart';

abstract class StorageService {
  Future<List<String>?> getStringList(String key);
  Future<bool> setStringList(String key, List<String> value);
}

class SharedPreferencesStorageService implements StorageService {
  @override
  Future<List<String>?> getStringList(String key) async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getStringList(key);
  }

  @override
  Future<bool> setStringList(String key, List<String> value) async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.setStringList(key, value);
  }
}
