import 'package:valora_app/services/property_photo_service.dart';

class FakePropertyPhotoService extends PropertyPhotoService {
  @override
  List<String> getPropertyPhotos({
    required double latitude,
    required double longitude,
    int limit = 3,
  }) {
    return <String>[];
  }
}
