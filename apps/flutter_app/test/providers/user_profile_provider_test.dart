import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/providers/user_profile_provider.dart';
import 'package:valora_app/models/user_profile.dart';
import '../mocks/mock_api_service.dart';

void main() {
  late UserProfileProvider provider;
  late MockApiService mockApiService;

  setUp(() {
    mockApiService = MockApiService();
    provider = UserProfileProvider(apiService: mockApiService);
  });

  group('UserProfileProvider', () {
    test('fetchProfile updates profile and loading state', () async {
      final user = UserProfile(email: 'test@example.com', defaultRadiusMeters: 1000, biometricsEnabled: false);
      when(mockApiService.getUserProfile()).thenAnswer((_) async => user);

      final future = provider.fetchProfile();
      expect(provider.isLoading, true);

      await future;
      expect(provider.isLoading, false);
      expect(provider.profile, user);
      expect(provider.error, null);
    });

    test('updateProfile calls API and refreshes', () async {
      when(mockApiService.updateProfile(
        firstName: anyNamed('firstName'),
        lastName: anyNamed('lastName'),
        defaultRadiusMeters: anyNamed('defaultRadiusMeters'),
        biometricsEnabled: anyNamed('biometricsEnabled'),
      )).thenAnswer((_) async {});

      final user = UserProfile(email: 'test@example.com', defaultRadiusMeters: 1000, biometricsEnabled: false);
      when(mockApiService.getUserProfile()).thenAnswer((_) async => user);

      final result = await provider.updateProfile(firstName: 'John');

      expect(result, true);
      verify(mockApiService.updateProfile(firstName: 'John', lastName: anyNamed('lastName'), defaultRadiusMeters: anyNamed('defaultRadiusMeters'), biometricsEnabled: anyNamed('biometricsEnabled'))).called(1);
    });
  });
}
