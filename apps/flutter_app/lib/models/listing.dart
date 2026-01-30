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
  final String? url;
  final String? imageUrl;

  Listing({
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
    this.url,
    this.imageUrl,
  });

  factory Listing.fromJson(Map<String, dynamic> json) {
    return Listing(
      id: json['id'],
      fundaId: json['fundaId'],
      address: json['address'],
      city: json['city'],
      postalCode: json['postalCode'],
      price: json['price']?.toDouble(),
      bedrooms: json['bedrooms'],
      bathrooms: json['bathrooms'],
      livingAreaM2: json['livingAreaM2'],
      propertyType: json['propertyType'],
      status: json['status'],
      url: json['url'],
      imageUrl: json['imageUrl'],
    );
  }
}
