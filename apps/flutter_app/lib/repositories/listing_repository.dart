import 'dart:convert';

import '../services/api_client.dart';
import '../models/listing.dart';
import '../models/listing_search_request.dart';

class ListingRepository {
  final ApiClient _apiClient;

  const ListingRepository(this._apiClient);

  Future<List<Listing>> searchListings(ListingSearchRequest request) async {
    final response = await _apiClient.get(
      '/listings/search',
      queryParameters: request.toQueryParameters(),
    );

    final data = jsonDecode(response.body) as List<dynamic>;
    return data
        .map((json) => Listing.fromJson(json as Map<String, dynamic>))
        .toList();
  }
}
