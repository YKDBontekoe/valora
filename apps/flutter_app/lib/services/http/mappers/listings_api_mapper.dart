import 'dart:convert';

import '../../../models/listing.dart';
import '../../../models/listing_response.dart';

class ListingsApiMapper {
  const ListingsApiMapper._();

  static ListingResponse parseListingResponse(String body) {
    return ListingResponse.fromJson(json.decode(body) as Map<String, dynamic>);
  }

  static Listing parseListing(String body) {
    return Listing.fromJson(json.decode(body) as Map<String, dynamic>);
  }
}
