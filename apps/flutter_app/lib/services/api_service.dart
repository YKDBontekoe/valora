import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import '../models/listing.dart';
import '../models/listing_response.dart';

class ApiService {
  static String get baseUrl {
    const fromEnv = String.fromEnvironment('API_BASE_URL');
    if (fromEnv.isNotEmpty) return fromEnv;
    return dotenv.env['API_BASE_URL'] ?? 'http://localhost:5000/api';
  }
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

  Future<ListingResponse> getListings({
    int page = 1,
    int pageSize = 10,
    String? searchTerm,
    double? minPrice,
    double? maxPrice,
    String? city,
    String? sortBy,
    String? sortOrder,
  }) async {
    try {
      final queryParams = <String, String>{
        'page': page.toString(),
        'pageSize': pageSize.toString(),
      };

      if (searchTerm != null && searchTerm.isNotEmpty) queryParams['searchTerm'] = searchTerm;
      if (minPrice != null) queryParams['minPrice'] = minPrice.toString();
      if (maxPrice != null) queryParams['maxPrice'] = maxPrice.toString();
      if (city != null && city.isNotEmpty) queryParams['city'] = city;
      if (sortBy != null && sortBy.isNotEmpty) queryParams['sortBy'] = sortBy;
      if (sortOrder != null && sortOrder.isNotEmpty) queryParams['sortOrder'] = sortOrder;

      final uri = Uri.parse('$baseUrl/listings').replace(queryParameters: queryParams);

      final response = await _client
          .get(uri)
          .timeout(timeoutDuration);

      return _handleResponse(response, (body) {
        return ListingResponse.fromJson(json.decode(body));
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
