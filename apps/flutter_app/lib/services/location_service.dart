import 'package:geolocator/geolocator.dart';

class LocationService {
  const LocationService();

  Future<Position> getCurrentLocation() async {
    bool serviceEnabled;
    LocationPermission permission;

    // Test if location services are enabled.
    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      throw const ValoraLocationServiceDisabledException();
    }

    permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        throw const ValoraPermissionDeniedException('Location permissions are denied');
      }
    }

    if (permission == LocationPermission.deniedForever) {
      throw const ValoraPermissionDeniedForeverException(
          'Location permissions are permanently denied, we cannot request permissions.');
    }

    return await Geolocator.getCurrentPosition();
  }
}

class ValoraLocationServiceDisabledException implements Exception {
  final String message;
  const ValoraLocationServiceDisabledException([this.message = 'Location services are disabled.']);
  @override
  String toString() => message;
}

class ValoraPermissionDeniedException implements Exception {
  final String message;
  const ValoraPermissionDeniedException([this.message = 'Location permissions are denied']);
  @override
  String toString() => message;
}

class ValoraPermissionDeniedForeverException implements Exception {
  final String message;
  const ValoraPermissionDeniedForeverException([this.message = 'Location permissions are permanently denied.']);
  @override
  String toString() => message;
}
