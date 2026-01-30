import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import '../models/listing.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:5000/api';
  static const Duration timeoutDuration = Duration(seconds: 10);

  final http.Client _client;

  ApiService({http.Client? client}) : _client = client ?? http.Client();

  Future<bool> healthCheck() async {
    try {
      final response = await _client
          .get(Uri.parse('$baseUrl/health'))
          .timeout(timeoutDuration);
      return response.statusCode == 200;
    } catch (e) {
      return false;
    }
  }

  Future<List<Listing>> getListings() async {
    try {
      final response = await _client
          .get(Uri.parse('$baseUrl/listings'))
          .timeout(timeoutDuration);

      return _handleResponse(response, (body) {
        final List<dynamic> data = json.decode(body);
        return data.map((json) => Listing.fromJson(json)).toList();
      });
    } on SocketException {
      throw Exception('No internet connection or server unreachable.');
    } on TimeoutException {
      throw Exception('Server request timed out.');
    } catch (e) {
      if (e is Exception) rethrow;
      throw Exception('Unexpected error: $e');
    }
  }

  Future<Listing?> getListing(String id) async {
    try {
      final response = await _client
          .get(Uri.parse('$baseUrl/listings/$id'))
          .timeout(timeoutDuration);

      if (response.statusCode == 404) {
        return null;
      }

      return _handleResponse(response, (body) {
        return Listing.fromJson(json.decode(body));
      });
    } on SocketException {
      throw Exception('No internet connection or server unreachable.');
    } on TimeoutException {
      throw Exception('Server request timed out.');
    } catch (e) {
      if (e is Exception) rethrow;
      throw Exception('Unexpected error: $e');
    }
  }

  T _handleResponse<T>(http.Response response, T Function(String body) parser) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return parser(response.body);
    } else if (response.statusCode >= 500) {
      throw Exception('Server error (500). Please try again later.');
    } else {
      throw Exception('Request failed with status: ${response.statusCode}');
    }
  }
}
