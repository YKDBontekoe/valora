import 'package:flutter/foundation.dart';
import '../models/listing_detail.dart';
import '../repositories/listing_repository.dart';

class ListingProvider extends ChangeNotifier {
  final ListingRepository _repository;

  ListingProvider(this._repository);

  bool _isLoading = false;
  bool get isLoading => _isLoading;

  ListingDetail? _listing;
  ListingDetail? get listing => _listing;

  String? _error;
  String? get error => _error;

  Future<void> loadListing(String id) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _listing = await _repository.getListingDetail(id);
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
