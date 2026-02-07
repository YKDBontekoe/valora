import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';

Map<String, dynamic> _legacyListingJson() {
  return <String, dynamic>{
    'id': 'listing-1',
    'fundaId': 'funda-1',
    'address': 'Teststraat 1',
    'city': 'Amsterdam',
    'price': 250000,
  };
}

Future<void> _waitForLoaded(FavoritesProvider provider) async {
  int attempts = 0;
  while (provider.isLoading && attempts < 30) {
    await Future<void>.delayed(const Duration(milliseconds: 10));
    attempts++;
  }
}

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues(<String, Object>{});
  });

  test(
    'migrates legacy favorites storage and preserves saved timestamp metadata',
    () async {
      SharedPreferences.setMockInitialValues(<String, Object>{
        'favorite_listings': <String>[json.encode(_legacyListingJson())],
      });

      final FavoritesProvider provider = FavoritesProvider();
      await _waitForLoaded(provider);

      expect(provider.favorites, hasLength(1));
      expect(provider.favorites.first.id, 'listing-1');
      expect(provider.savedAtFor('listing-1'), isNotNull);
    },
  );

  test('writes v2 records when toggling favorites', () async {
    final FavoritesProvider provider = FavoritesProvider();
    await _waitForLoaded(provider);

    final Listing listing = Listing(
      id: 'listing-99',
      fundaId: 'funda-99',
      address: 'Nieuwe Straat 10',
      city: 'Utrecht',
      price: 300000,
      description: 'Should not be persisted in summary format',
    );

    await provider.toggleFavorite(listing);

    final SharedPreferences prefs = await SharedPreferences.getInstance();
    final List<String> persisted =
        prefs.getStringList('favorite_listings_v2') ?? <String>[];

    expect(persisted, hasLength(1));

    final Map<String, dynamic> record =
        json.decode(persisted.first) as Map<String, dynamic>;
    expect(record['savedAt'], isNotNull);

    final Map<String, dynamic> storedListing =
        record['listing'] as Map<String, dynamic>;
    expect(storedListing['id'], 'listing-99');
    expect(storedListing['address'], 'Nieuwe Straat 10');
    expect(storedListing['description'], isNull);
  });
}
