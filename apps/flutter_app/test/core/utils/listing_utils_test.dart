import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/utils/listing_utils.dart';
import 'package:valora_app/core/theme/valora_colors.dart';

void main() {
  group('ListingUtils', () {
    test('getStatusColor returns correct color for status', () {
      expect(ListingUtils.getStatusColor('New'), ValoraColors.newBadge);
      expect(ListingUtils.getStatusColor('new'), ValoraColors.newBadge);
      expect(ListingUtils.getStatusColor('Sold'), ValoraColors.soldBadge);
      expect(ListingUtils.getStatusColor('sold'), ValoraColors.soldBadge);
      expect(ListingUtils.getStatusColor('Under Offer'), ValoraColors.soldBadge);
      expect(ListingUtils.getStatusColor('under offer'), ValoraColors.soldBadge);
      expect(ListingUtils.getStatusColor('For Sale'), ValoraColors.primary);
      expect(ListingUtils.getStatusColor('Unknown'), ValoraColors.primary);
    });

    test('getScoreColor returns correct color for score', () {
      expect(ListingUtils.getScoreColor(80.0), ValoraColors.success);
      expect(ListingUtils.getScoreColor(85.5), ValoraColors.success);
      expect(ListingUtils.getScoreColor(100.0), ValoraColors.success);

      expect(ListingUtils.getScoreColor(60.0), ValoraColors.primary);
      expect(ListingUtils.getScoreColor(79.9), ValoraColors.primary);

      expect(ListingUtils.getScoreColor(40.0), ValoraColors.warning);
      expect(ListingUtils.getScoreColor(59.9), ValoraColors.warning);

      expect(ListingUtils.getScoreColor(39.9), ValoraColors.error);
      expect(ListingUtils.getScoreColor(0.0), ValoraColors.error);
    });
  });
}
