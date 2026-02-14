import 'package:mockito/mockito.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/models/listing_response.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:valora_app/models/user_profile.dart';
import 'package:valora_app/models/map_city_insight.dart';

class MockApiService extends Mock implements ApiService {
  @override
  Future<ListingResponse> getListings(ListingFilter filter) async {
    return ListingResponse(items: [], totalCount: 0, pageIndex: 1, totalPages: 0, hasNextPage: false, hasPreviousPage: false);
  }

  @override
  Future<List<MapCityInsight>> getCityInsights() async {
    return [];
  }

  @override
  Future<UserProfile> getUserProfile() async {
    return UserProfile(email: 'test@example.com', defaultRadiusMeters: 1000, biometricsEnabled: false);
  }
}
