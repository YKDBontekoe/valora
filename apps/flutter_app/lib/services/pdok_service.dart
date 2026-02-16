import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

class PdokSuggestion {
  final String id;
  final String type;
  final String displayName;
  final double score;

  PdokSuggestion({
    required this.id,
    required this.type,
    required this.displayName,
    required this.score,
  });

  factory PdokSuggestion.fromJson(Map<String, dynamic> json) {
    return PdokSuggestion(
      id: json['id'] as String,
      type: json['type'] as String,
      displayName: json['weergavenaam'] as String,
      score: (json['score'] as num).toDouble(),
    );
  }
}

class PdokService {
  static const String _authority = 'api.pdok.nl';
  static const String _path = '/bzk/locatieserver/search/v3_1/suggest';
  static const String _reversePath = '/bzk/locatieserver/search/v3_1/reverse';

  Future<List<PdokSuggestion>> search(String query) async {
    if (query.isEmpty) {
      return [];
    }

    try {
      final uri = Uri.https(_authority, _path, {
        'q': query,
        'rows': '5',
        'fq': 'type:(woonplaats OR weg OR adres OR postcode)',
      });
      
      final response = await http.get(uri).timeout(const Duration(seconds: 10));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        final docs = data['response']['docs'] as List;
        return docs.map((doc) => PdokSuggestion.fromJson(doc)).toList();
      } else {
        debugPrint('PDOK Error: ${response.statusCode}');
        return [];
      }
    } catch (e) {
      debugPrint('PDOK request failed: $e');
      return [];
    }
  }

  Future<String?> reverseLookup(double lat, double lon) async {
    try {
      final uri = Uri.https(_authority, _reversePath, {
        'lat': lat.toString(),
        'lon': lon.toString(),
        'rows': '1',
        'type': 'adres',
      });

      final response = await http.get(uri).timeout(const Duration(seconds: 10));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        final docs = data['response']['docs'] as List;
        if (docs.isNotEmpty) {
          return docs[0]['weergavenaam'] as String;
        }
      }
    } catch (e) {
      debugPrint('PDOK reverse lookup failed: $e');
    }
    return null;
  }
}
