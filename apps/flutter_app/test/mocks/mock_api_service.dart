import 'package:mockito/mockito.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/models/listing_response.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:valora_app/models/user_profile.dart';
import 'package:valora_app/models/map_city_insight.dart';

class MockApiService extends Mock implements ApiService {
  @override
  Future<ListingResponse> getListings(ListingFilter? filter) async {
    return ListingResponse(items: [], totalCount: 0, pageIndex: 1, totalPages: 0, hasNextPage: false, hasPreviousPage: false);
  }

  @override
  Future<List<MapCityInsight>> getCityInsights() async {
    return [];
  }

  @override
  Future<UserProfile> getUserProfile() async {
    return super.noSuchMethod(Invocation.method(#getUserProfile, []), returnValue: Future.value(UserProfile(email: 'test@example.com', defaultRadiusMeters: 1000, biometricsEnabled: false)));
  }

  @override
  Future<void> updateProfile({String? firstName, String? lastName, int? defaultRadiusMeters, bool? biometricsEnabled}) async {
    return super.noSuchMethod(Invocation.method(#updateProfile, [], {#firstName: firstName, #lastName: lastName, #defaultRadiusMeters: defaultRadiusMeters, #biometricsEnabled: biometricsEnabled}), returnValue: Future.value());
  }

  @override
  Future<void> changePassword(String? currentPassword, String? newPassword, String? confirmNewPassword) async {
    return super.noSuchMethod(Invocation.method(#changePassword, [currentPassword, newPassword, confirmNewPassword]), returnValue: Future.value());
  }
}
