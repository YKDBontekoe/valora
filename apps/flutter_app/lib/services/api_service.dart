import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/listing.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:5000/api';

  Future<bool> healthCheck() async {
    try {
      final response = await http.get(Uri.parse('$baseUrl/health'));
      return response.statusCode == 200;
    } catch (e) {
      return false;
    }
  }

  Future<List<Listing>> getListings() async {
    final response = await http.get(Uri.parse('$baseUrl/listings'));
    if (response.statusCode == 200) {
      final List<dynamic> data = json.decode(response.body);
      return data.map((json) => Listing.fromJson(json)).toList();
    }
    throw Exception('Failed to load listings');
  }

  Future<Listing?> getListing(String id) async {
    final response = await http.get(Uri.parse('$baseUrl/listings/$id'));
    if (response.statusCode == 200) {
      return Listing.fromJson(json.decode(response.body));
    } else if (response.statusCode == 404) {
      return null;
    }
    throw Exception('Failed to load listing');
  }
}
