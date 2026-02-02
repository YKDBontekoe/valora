import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/saved_listings_screen.dart';
import 'package:valora_app/widgets/home_components.dart';

class FakeFavoritesProvider extends ChangeNotifier implements FavoritesProvider {
  @override
  List<Listing> get favorites => _favorites;
  List<Listing> _favorites = [];

  @override
  bool get isLoading => _isLoading;
  bool _isLoading = false;

  void setFavorites(List<Listing> list) {
    _favorites = list;
    notifyListeners();
  }

  void setLoading(bool loading) {
    _isLoading = loading;
    notifyListeners();
  }

  @override
  Future<void> loadFavorites() async {}

  @override
  Future<void> toggleFavorite(Listing listing) async {
    if (isFavorite(listing.id)) {
      _favorites.removeWhere((l) => l.id == listing.id);
    } else {
      _favorites.add(listing);
    }
    notifyListeners();
  }

  @override
  bool isFavorite(String id) => _favorites.any((l) => l.id == id);
}

void main() {
  Widget createScreen(FavoritesProvider provider) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>.value(value: provider),
      ],
      child: const MaterialApp(
        home: SavedListingsScreen(),
      ),
    );
  }

  group('SavedListingsScreen', () {
    testWidgets('shows loading state', (WidgetTester tester) async {
      final provider = FakeFavoritesProvider();
      provider.setLoading(true);

      await tester.pumpWidget(createScreen(provider));
      // Use pump(Duration) instead of pumpAndSettle to avoid timeout with CircularProgressIndicator
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('shows empty state when no favorites', (WidgetTester tester) async {
      final provider = FakeFavoritesProvider();
      provider.setLoading(false);
      provider.setFavorites([]);

      await tester.pumpWidget(createScreen(provider));
      await tester.pumpAndSettle();

      expect(find.text('No saved listings'), findsOneWidget);
      expect(find.byType(NearbyListingCard), findsNothing);
    });

    testWidgets('shows saved listings', (WidgetTester tester) async {
      final provider = FakeFavoritesProvider();
      provider.setLoading(false);
      final listing = Listing(
        id: '1',
        fundaId: 'f1',
        address: 'Test Addr',
        price: 200000
      );
      provider.setFavorites([listing]);

      await tester.pumpWidget(createScreen(provider));
      await tester.pumpAndSettle();

      expect(find.text('No saved listings'), findsNothing);
      expect(find.byType(NearbyListingCard), findsOneWidget);
      expect(find.text('Test Addr'), findsOneWidget);
    });
  });
}
