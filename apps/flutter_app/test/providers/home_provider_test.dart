import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/filter_chip_model.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:valora_app/providers/home_provider.dart';

void main() {
  group('HomeProvider', () {
    test('toggles quick filters and exposes chip state', () {
      final provider = HomeProvider();

      expect(provider.isQuickFilterActive(HomeQuickFilter.under500k), isFalse);
      provider.toggleQuickFilter(HomeQuickFilter.under500k);

      expect(provider.isQuickFilterActive(HomeQuickFilter.under500k), isTrue);
      expect(provider.activeQuickFilterCount, 1);

      final chip = provider.quickFilterChips.firstWhere(
        (element) => element.filter == HomeQuickFilter.under500k,
      );
      expect(chip.isActive, isTrue);

      provider.toggleQuickFilter(HomeQuickFilter.under500k);
      expect(provider.isQuickFilterActive(HomeQuickFilter.under500k), isFalse);
      expect(provider.activeQuickFilterCount, 0);
    });

    test('applies quick filter constraints to listing filter', () {
      final provider = HomeProvider();
      provider
        ..toggleQuickFilter(HomeQuickFilter.under500k)
        ..toggleQuickFilter(HomeQuickFilter.threePlusBeds);

      final baseFilter = ListingFilter(
        maxPrice: 750000,
        minBedrooms: 1,
      );

      final updated = provider.applyQuickFilters(baseFilter);

      expect(updated.maxPrice, 500000);
      expect(updated.minBedrooms, 3);
    });

    test('keeps stricter base filters when applying quick filters', () {
      final provider = HomeProvider();
      provider
        ..toggleQuickFilter(HomeQuickFilter.under500k)
        ..toggleQuickFilter(HomeQuickFilter.threePlusBeds);

      final baseFilter = ListingFilter(
        maxPrice: 300000,
        minBedrooms: 4,
      );

      final updated = provider.applyQuickFilters(baseFilter);

      expect(updated.maxPrice, 300000);
      expect(updated.minBedrooms, 4);
    });
  });
}
