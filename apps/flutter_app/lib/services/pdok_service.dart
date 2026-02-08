import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

class PdokSuggestion {
  final String id;
  final String type;
  final String weergavenaam;
  final double score;

  PdokSuggestion({
    required this.id,
    required this.type,
    required this.weergavenaam,
    required this.score,
  });

  factory PdokSuggestion.fromJson(Map<String, dynamic> json) {
    return PdokSuggestion(
      id: json['id'] as String,
      type: json['type'] as String,
      weergavenaam: json['weergavenaam'] as String,
      score: (json['score'] as num).toDouble(),
    );
  }
}

class PdokService {
  static const String _baseUrl =
      'https://api.pdok.nl/bzk/locatieserver/search/v3_1/suggest';

  Future<List<PdokSuggestion>> search(String query) async {
    if (query.isEmpty) {
      return [];
    }

    try {
      final response = await http.get(
        Uri.parse(
          '$_baseUrl?q=$query&rows=5&fq=type:(woonplaats OR weg OR adres OR postcode)',
        ),
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        final docs = data['response']['docs'] as List;
        return docs.map((doc) => PdokSuggestion.fromJson(doc)).toList();
      } else {
        debugPrint('PDOK Error: ${response.statusCode}');
        return [];
      }
    } catch (e) {
      debugPrint('PDOK Exception: $e');
      return [];
    }
  }
}
