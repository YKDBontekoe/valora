import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/search_history_item.dart';

class SearchHistoryService {
  static const String _key = 'search_history';
  static const int _maxHistory = 10;

  Future<List<SearchHistoryItem>> getHistory() async {
    final prefs = await SharedPreferences.getInstance();
    final String? jsonString = prefs.getString(_key);
    if (jsonString == null) {
      return [];
    }

    try {
      final List<dynamic> jsonList = json.decode(jsonString);
      return jsonList
          .map((e) => SearchHistoryItem.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (e) {
      return [];
    }
  }

  Future<void> addToHistory(String query) async {
    final prefs = await SharedPreferences.getInstance();
    final List<SearchHistoryItem> history = await getHistory();

    // Remove existing item if same query (case-insensitive)
    history.removeWhere(
      (item) => item.query.toLowerCase() == query.toLowerCase(),
    );

    // Add new item to top
    history.insert(
      0,
      SearchHistoryItem(query: query, timestamp: DateTime.now()),
    );

    // Limit size
    if (history.length > _maxHistory) {
      history.removeRange(_maxHistory, history.length);
    }

    await prefs.setString(
      _key,
      json.encode(history.map((e) => e.toJson()).toList()),
    );
  }

  Future<void> removeFromHistory(String query) async {
    final prefs = await SharedPreferences.getInstance();
    final List<SearchHistoryItem> history = await getHistory();

    history.removeWhere((item) => item.query == query);

    await prefs.setString(
      _key,
      json.encode(history.map((e) => e.toJson()).toList()),
    );
  }

  Future<void> clearHistory() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_key);
  }
}
