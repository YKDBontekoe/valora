import 'package:latlong2/latlong.dart';
import 'map_amenity.dart';

class PropertyDetail {
  final String id;
  final String? fundaId;
  final double? price;
  final String address;
  final String? city;
  final String? postalCode;
  final int? bedrooms;
  final int? bathrooms;
  final int? livingAreaM2;
  final String? energyLabel;
  final String? description;
  final List<String> imageUrls;
  final double? contextCompositeScore;
  final double? contextSafetyScore;
  final double? contextSocialScore;
  final double? contextAmenitiesScore;
  final double? contextEnvironmentScore;
  final double? pricePerM2;
  final double? neighborhoodAvgPriceM2;
  final double? pricePercentile;
  final List<MapAmenity> nearbyAmenities;
  final LatLng? location;

  PropertyDetail({
    required this.id,
    this.fundaId,
    this.price,
    required this.address,
    this.city,
    this.postalCode,
    this.bedrooms,
    this.bathrooms,
    this.livingAreaM2,
    this.energyLabel,
    this.description,
    this.imageUrls = const [],
    this.contextCompositeScore,
    this.contextSafetyScore,
    this.contextSocialScore,
    this.contextAmenitiesScore,
    this.contextEnvironmentScore,
    this.pricePerM2,
    this.neighborhoodAvgPriceM2,
    this.pricePercentile,
    this.nearbyAmenities = const [],
    this.location,
  });

  factory PropertyDetail.fromJson(Map<String, dynamic> json) {
    return PropertyDetail(
      id: json['id'],
      fundaId: json['fundaId'],
      price: (json['price'] as num?)?.toDouble(),
      address: json['address'] ?? '',
      city: json['city'],
      postalCode: json['postalCode'],
      bedrooms: json['bedrooms'],
      bathrooms: json['bathrooms'],
      livingAreaM2: json['livingAreaM2'],
      energyLabel: json['energyLabel'],
      description: json['description'],
      imageUrls: (json['imageUrls'] as List?)?.map((e) => e.toString()).toList() ?? [],
      contextCompositeScore: (json['contextCompositeScore'] as num?)?.toDouble(),
      contextSafetyScore: (json['contextSafetyScore'] as num?)?.toDouble(),
      contextSocialScore: (json['contextSocialScore'] as num?)?.toDouble(),
      contextAmenitiesScore: (json['contextAmenitiesScore'] as num?)?.toDouble(),
      contextEnvironmentScore: (json['contextEnvironmentScore'] as num?)?.toDouble(),
      pricePerM2: (json['pricePerM2'] as num?)?.toDouble(),
      neighborhoodAvgPriceM2: (json['neighborhoodAvgPriceM2'] as num?)?.toDouble(),
      pricePercentile: (json['pricePercentile'] as num?)?.toDouble(),
      nearbyAmenities: (json['nearbyAmenities'] as List?)
              ?.map((e) => MapAmenity.fromJson(e))
              .toList() ??
          [],
      location: json['latitude'] != null && json['longitude'] != null
          ? LatLng(json['latitude'], json['longitude'])
          : null,
    );
  }
}
