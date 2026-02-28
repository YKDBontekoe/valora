import 'dart:convert';
import 'package:http/http.dart' as http;
import '../core/config/app_config.dart';
import '../services/auth_service.dart';
import '../models/listing_detail.dart';

class ListingRepository {
  final AuthService _authService;

  ListingRepository(this._authService);

  Future<ListingDetail> getListingDetail(String id) async {
    final token = await _authService.getToken();
    final response = await http.get(
      Uri.parse('${AppConfig.apiUrl}/listings/$id'),
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      return ListingDetail.fromJson(json);
    } else if (response.statusCode == 404) {
      throw Exception('Listing not found');
    } else {
      throw Exception('Failed to load listing details: ${response.statusCode}');
    }
  }
}
