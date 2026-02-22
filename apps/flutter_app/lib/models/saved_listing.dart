class SavedListing {
  final String id;
  final String listingId;
  final ListingSummary? listing;
  final String addedByUserId;
  final String? notes;
  final DateTime addedAt;
  final int commentCount;

  SavedListing({
    required this.id,
    required this.listingId,
    this.listing,
    required this.addedByUserId,
    this.notes,
    required this.addedAt,
    required this.commentCount,
  });

  factory SavedListing.fromJson(Map<String, dynamic> json) {
    return SavedListing(
      id: json['id'],
      listingId: json['listingId'],
      listing: json['listing'] != null ? ListingSummary.fromJson(json['listing']) : null,
      addedByUserId: json['addedByUserId'],
      notes: json['notes'],
      addedAt: DateTime.parse(json['addedAt']),
      commentCount: json['commentCount'],
    );
  }
}

class ListingSummary {
  final String id;
  final String address;
  final String? city;
  final double? price;
  final String? imageUrl;
  final int? bedrooms;
  final int? livingAreaM2;

  ListingSummary({
    required this.id,
    required this.address,
    this.city,
    this.price,
    this.imageUrl,
    this.bedrooms,
    this.livingAreaM2,
  });

  factory ListingSummary.fromJson(Map<String, dynamic> json) {
    return ListingSummary(
      id: json['id'],
      address: json['address'],
      city: json['city'],
      price: json['price'] != null ? (json['price'] as num).toDouble() : null,
      imageUrl: json['imageUrl'],
      bedrooms: json['bedrooms'],
      livingAreaM2: json['livingAreaM2'],
    );
  }
}
