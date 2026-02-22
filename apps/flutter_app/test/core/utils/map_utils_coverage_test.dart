import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/utils/map_utils.dart';

void main() {
  test('parsePolygonGeometry handles malformed geometry gracefully', () {
    // Should log error but return empty list
    final invalid = {'type': 'Polygon', 'coordinates': 'not a list'};

    expect(MapUtils.parsePolygonGeometry(invalid), isEmpty);
  });

  test('parsePolygonGeometry handles invalid coordinates gracefully', () {
    final invalid = {
      'type': 'Polygon',
      'coordinates': [
        [
          ['not', 'numbers'],
        ],
      ],
    };

    expect(MapUtils.parsePolygonGeometry(invalid), isEmpty);
  });
}
