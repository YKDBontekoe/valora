import '../models/context_report.dart';

class ListingDetail {
  final String id;
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
  final String? description;
  final String? energyLabel;
  final int? yearBuilt;
  final List<String> imageUrls;
  final double? latitude;
  final double? longitude;

  final double? contextCompositeScore;
  final double? contextSafetyScore;
  final double? contextSocialScore;
  final double? contextAmenitiesScore;
  final double? contextEnvironmentScore;

  final ContextReport? contextReport;

  ListingDetail({
    required this.id,
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
    this.description,
    this.energyLabel,
    this.yearBuilt,
    this.imageUrls = const [],
    this.latitude,
    this.longitude,
    this.contextCompositeScore,
    this.contextSafetyScore,
    this.contextSocialScore,
    this.contextAmenitiesScore,
    this.contextEnvironmentScore,
    this.contextReport,
  });

  factory ListingDetail.fromJson(Map<String, dynamic> json) {
    return ListingDetail(
      id: json['id'] as String,
      address: json['address'] as String,
      city: json['city'] as String?,
      postalCode: json['postalCode'] as String?,
      price: json['price'] != null ? (json['price'] as num).toDouble() : null,
      bedrooms: json['bedrooms'] as int?,
      bathrooms: json['bathrooms'] as int?,
      livingAreaM2: json['livingAreaM2'] as int?,
      plotAreaM2: json['plotAreaM2'] as int?,
      propertyType: json['propertyType'] as String?,
      status: json['status'] as String?,
      url: json['url'] as String?,
      imageUrl: json['imageUrl'] as String?,
      listedDate: json['listedDate'] != null
          ? DateTime.parse(json['listedDate'] as String)
          : null,
      description: json['description'] as String?,
      energyLabel: json['energyLabel'] as String?,
      yearBuilt: json['yearBuilt'] as int?,
      imageUrls: (json['imageUrls'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          const [],
      latitude: json['latitude'] != null ? (json['latitude'] as num).toDouble() : null,
      longitude: json['longitude'] != null ? (json['longitude'] as num).toDouble() : null,
      contextCompositeScore: json['contextCompositeScore'] != null ? (json['contextCompositeScore'] as num).toDouble() : null,
      contextSafetyScore: json['contextSafetyScore'] != null ? (json['contextSafetyScore'] as num).toDouble() : null,
      contextSocialScore: json['contextSocialScore'] != null ? (json['contextSocialScore'] as num).toDouble() : null,
      contextAmenitiesScore: json['contextAmenitiesScore'] != null ? (json['contextAmenitiesScore'] as num).toDouble() : null,
      contextEnvironmentScore: json['contextEnvironmentScore'] != null ? (json['contextEnvironmentScore'] as num).toDouble() : null,
      contextReport: json['contextReport'] != null
          ? ContextReport.fromJson(json['contextReport'] as Map<String, dynamic>)
          : null,
    );
  }
}
