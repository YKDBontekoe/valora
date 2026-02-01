import 'dart:async';
import 'dart:convert';
import 'dart:developer' as developer;
import 'dart:io';
import 'package:http/http.dart' as http;
import '../core/exceptions/app_exceptions.dart';
import '../models/listing.dart';
import '../models/listing_response.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:5000/api';
  static const Duration timeoutDuration = Duration(seconds: 10);

  final http.Client _client;
  final String? _authToken;

  ApiService({http.Client? client, String? authToken})
      : _client = client ?? http.Client(),
        _authToken = authToken;

  Map<String, String> get _headers => {
    if (_authToken != null) 'Authorization': 'Bearer $_authToken',
    'Content-Type': 'application/json',
  };

  Future<bool> healthCheck() async {
    try {
      final response = await _client
          .get(Uri.parse('$baseUrl/health'), headers: _headers)
          .timeout(timeoutDuration);
      return response.statusCode == 200;
    } catch (e) {
      developer.log('Health check failed: $e', name: 'ApiService');
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
    int? minBedrooms,
    int? minLivingArea,
    int? maxLivingArea,
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
      if (minBedrooms != null) queryParams['minBedrooms'] = minBedrooms.toString();
      if (minLivingArea != null) queryParams['minLivingArea'] = minLivingArea.toString();
      if (maxLivingArea != null) queryParams['maxLivingArea'] = maxLivingArea.toString();
      if (sortBy != null && sortBy.isNotEmpty) queryParams['sortBy'] = sortBy;
      if (sortOrder != null && sortOrder.isNotEmpty) queryParams['sortOrder'] = sortOrder;

      final uri = Uri.parse('$baseUrl/listings').replace(queryParameters: queryParams);

      final response = await _client
          .get(uri, headers: _headers)
          .timeout(timeoutDuration);

      return _handleResponse(response, (body) {
        return ListingResponse.fromJson(json.decode(body));
      });
    } catch (e) {
      throw _handleException(e);
    }
  }

  Future<Listing?> getListing(String id) async {
    try {
      final response = await _client
          .get(Uri.parse('$baseUrl/listings/$id'), headers: _headers)
          .timeout(timeoutDuration);

      if (response.statusCode == 404) {
        return null;
      }

      return _handleResponse(response, (body) {
        return Listing.fromJson(json.decode(body));
      });
    } catch (e) {
      throw _handleException(e);
    }
  }

  Future<void> triggerLimitedScrape(String region, int limit) async {
    try {
      final queryParams = <String, String>{
        'region': region,
        'limit': limit.toString(),
      };

      final uri = Uri.parse('$baseUrl/scraper/trigger-limited').replace(queryParameters: queryParams);

      final response = await _client
          .post(uri)
          .timeout(timeoutDuration);

      _handleResponse(response, (_) => null);
    } catch (e) {
      throw _handleException(e);
    }
  }

  T _handleResponse<T>(http.Response response, T Function(String body) parser) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return parser(response.body);
    }

    developer.log(
      'API Error: ${response.statusCode} - ${response.body}',
      name: 'ApiService',
    );

    switch (response.statusCode) {
      case 400:
        throw ValidationException(_parseErrorMessage(response.body) ?? 'Invalid request');
      case 404:
        throw NotFoundException('Resource not found');
      case 500:
      case 502:
      case 503:
      case 504:
        throw ServerException('Server error (${response.statusCode}). Please try again later.');
      default:
        throw UnknownException('Request failed with status: ${response.statusCode}');
    }
  }

  Exception _handleException(dynamic error) {
    if (error is AppException) return error;

    developer.log('Network Error: $error', name: 'ApiService');

    if (error is SocketException) {
      return NetworkException('No internet connection or server unreachable.');
    } else if (error is TimeoutException) {
      return NetworkException('Server request timed out.');
    } else if (error is http.ClientException) {
      return NetworkException('Connection failed. Please check your network.');
    }

    return UnknownException('Unexpected error: $error');
  }

  String? _parseErrorMessage(String body) {
    try {
      final jsonBody = json.decode(body);
      if (jsonBody is Map<String, dynamic>) {
        return jsonBody['detail'] as String? ?? jsonBody['title'] as String?;
      }
    } catch (_) {
      // Ignore parsing errors
    }
    return null;
  }
}
