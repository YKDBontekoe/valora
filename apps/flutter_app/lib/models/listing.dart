class Listing {
  final String id;
  final String fundaId;
  final String address;
  final String? city;
  final String? postalCode;
  final double? price;
  final int? bedrooms;
  final int? bathrooms;
  final int? livingAreaM2;
  final String? propertyType;
  final String? status;
  final String? imageUrl;
  final DateTime? listedDate;
  final String? energyLabel;
  final int? yearBuilt;
  final double? latitude;
  final double? longitude;
  final double? contextCompositeScore;

  const Listing({
    required this.id,
    required this.fundaId,
    required this.address,
    this.city,
    this.postalCode,
    this.price,
    this.bedrooms,
    this.bathrooms,
    this.livingAreaM2,
    this.propertyType,
    this.status,
    this.imageUrl,
    this.listedDate,
    this.energyLabel,
    this.yearBuilt,
    this.latitude,
    this.longitude,
    this.contextCompositeScore,
  });

  factory Listing.fromJson(Map<String, dynamic> json) {
    return Listing(
      id: json['id'] as String,
      fundaId: json['fundaId'] as String,
      address: json['address'] as String,
      city: json['city'] as String?,
      postalCode: json['postalCode'] as String?,
      price: json['price'] != null ? (json['price'] as num).toDouble() : null,
      bedrooms: json['bedrooms'] as int?,
      bathrooms: json['bathrooms'] as int?,
      livingAreaM2: json['livingAreaM2'] as int?,
      propertyType: json['propertyType'] as String?,
      status: json['status'] as String?,
      imageUrl: json['imageUrl'] as String?,
      listedDate: json['listedDate'] != null
          ? DateTime.parse(json['listedDate'] as String)
          : null,
      energyLabel: json['energyLabel'] as String?,
      yearBuilt: json['yearBuilt'] as int?,
      latitude: json['latitude'] != null ? (json['latitude'] as num).toDouble() : null,
      longitude: json['longitude'] != null ? (json['longitude'] as num).toDouble() : null,
      contextCompositeScore: json['contextCompositeScore'] != null ? (json['contextCompositeScore'] as num).toDouble() : null,
    );
  }
}
