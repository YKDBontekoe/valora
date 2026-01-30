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
  final int? plotAreaM2;
  final String? propertyType;
  final String? status;
  final String? url;
  final String? imageUrl;
  final DateTime? listedDate;
  final DateTime? createdAt;

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
    this.plotAreaM2,
    this.propertyType,
    this.status,
    this.url,
    this.imageUrl,
    this.listedDate,
    this.createdAt,
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
      plotAreaM2: json['plotAreaM2'],
      propertyType: json['propertyType'],
      status: json['status'],
      url: json['url'],
      imageUrl: json['imageUrl'],
      listedDate: json['listedDate'] != null ? DateTime.parse(json['listedDate']) : null,
      createdAt: json['createdAt'] != null ? DateTime.parse(json['createdAt']) : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'fundaId': fundaId,
      'address': address,
      'city': city,
      'postalCode': postalCode,
      'price': price,
      'bedrooms': bedrooms,
      'bathrooms': bathrooms,
      'livingAreaM2': livingAreaM2,
      'plotAreaM2': plotAreaM2,
      'propertyType': propertyType,
      'status': status,
      'url': url,
      'imageUrl': imageUrl,
      'listedDate': listedDate?.toIso8601String(),
      'createdAt': createdAt?.toIso8601String(),
    };
  }
}
