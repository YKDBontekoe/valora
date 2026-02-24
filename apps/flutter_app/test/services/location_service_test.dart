import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';
import 'package:plugin_platform_interface/plugin_platform_interface.dart';
import 'package:valora_app/services/location_service.dart';

class MockGeolocatorPlatform extends GeolocatorPlatform with MockPlatformInterfaceMixin {
  bool serviceEnabled = true;
  LocationPermission permission = LocationPermission.whileInUse;
  Position position = Position(
    longitude: 4.89,
    latitude: 52.37,
    timestamp: DateTime.now(),
    accuracy: 10,
    altitude: 0,
    heading: 0,
    speed: 0,
    speedAccuracy: 0,
    altitudeAccuracy: 0,
    headingAccuracy: 0,
  );

  @override
  Future<bool> isLocationServiceEnabled() async => serviceEnabled;

  @override
  Future<LocationPermission> checkPermission() async => permission;

  @override
  Future<LocationPermission> requestPermission() async => permission;

  @override
  Future<Position> getCurrentPosition({LocationSettings? locationSettings}) async => position;
}

void main() {
  group('LocationService', () {
    late LocationService service;
    late MockGeolocatorPlatform mockGeolocator;

    setUp(() {
      mockGeolocator = MockGeolocatorPlatform();
      GeolocatorPlatform.instance = mockGeolocator;
      service = const LocationService();
    });

    test('getCurrentLocation returns position when permission granted', () async {
      final position = await service.getCurrentLocation();
      expect(position.latitude, 52.37);
      expect(position.longitude, 4.89);
    });

    test('throws ValoraLocationServiceDisabledException when service disabled', () async {
      mockGeolocator.serviceEnabled = false;
      expect(
        () => service.getCurrentLocation(),
        throwsA(isA<ValoraLocationServiceDisabledException>()),
      );
    });

    test('throws ValoraPermissionDeniedException when permission denied', () async {
      mockGeolocator.permission = LocationPermission.denied;
      expect(
        () => service.getCurrentLocation(),
        throwsA(isA<ValoraPermissionDeniedException>()),
      );
    });

    test('throws ValoraPermissionDeniedForeverException when permission denied forever', () async {
      mockGeolocator.permission = LocationPermission.deniedForever;
      expect(
        () => service.getCurrentLocation(),
        throwsA(isA<ValoraPermissionDeniedForeverException>()),
      );
    });
  });
}
