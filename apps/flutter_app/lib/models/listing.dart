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
  
  // Rich Data
  final String? description;
  final String? energyLabel;
  final int? yearBuilt;
  final List<String> imageUrls;
  
  // Phase 2
  final String? ownershipType;
  final String? cadastralDesignation;
  final double? vveContribution;
  final String? heatingType;
  final String? insulationType;
  final String? gardenOrientation;
  final bool hasGarage;
  final String? parkingType;
  
  // Phase 3
  final String? agentName;
  final int? volumeM3;
  final int? balconyM2;
  final int? gardenM2;
  final int? externalStorageM2;
  final Map<String, String> features;
  
  // Geo & Media
  final double? latitude;
  final double? longitude;
  final String? videoUrl;
  final String? virtualTourUrl;
  final List<String> floorPlanUrls;
  final String? brochureUrl;
  
  // Construction
  final String? roofType;
  final int? numberOfFloors;
  final String? constructionPeriod;
  final String? cvBoilerBrand;
  final int? cvBoilerYear;
  
  // Broker
  final String? brokerPhone;
  final String? brokerLogoUrl;
  
  // Infra
  final bool? fiberAvailable;
  
  // Status
  final DateTime? publicationDate;
  final bool isSoldOrRented;
  final List<String> labels;

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
    this.description,
    this.energyLabel,
    this.yearBuilt,
    this.imageUrls = const [],
    this.ownershipType,
    this.cadastralDesignation,
    this.vveContribution,
    this.heatingType,
    this.insulationType,
    this.gardenOrientation,
    this.hasGarage = false,
    this.parkingType,
    this.agentName,
    this.volumeM3,
    this.balconyM2,
    this.gardenM2,
    this.externalStorageM2,
    this.features = const {},
    this.latitude,
    this.longitude,
    this.videoUrl,
    this.virtualTourUrl,
    this.floorPlanUrls = const [],
    this.brochureUrl,
    this.roofType,
    this.numberOfFloors,
    this.constructionPeriod,
    this.cvBoilerBrand,
    this.cvBoilerYear,
    this.brokerPhone,
    this.brokerLogoUrl,
    this.fiberAvailable,
    this.publicationDate,
    this.isSoldOrRented = false,
    this.labels = const [],
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
      
      description: json['description'],
      energyLabel: json['energyLabel'],
      yearBuilt: json['yearBuilt'],
      imageUrls: (json['imageUrls'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      
      ownershipType: json['ownershipType'],
      cadastralDesignation: json['cadastralDesignation'],
      vveContribution: json['vveContribution']?.toDouble(),
      heatingType: json['heatingType'],
      insulationType: json['insulationType'],
      gardenOrientation: json['gardenOrientation'],
      hasGarage: json['hasGarage'] ?? false,
      parkingType: json['parkingType'],
      
      agentName: json['agentName'],
      volumeM3: json['volumeM3'],
      balconyM2: json['balconyM2'],
      gardenM2: json['gardenM2'],
      externalStorageM2: json['externalStorageM2'],
      features: (json['features'] as Map<String, dynamic>?)?.map((k, v) => MapEntry(k, v.toString())) ?? {},
      
      latitude: json['latitude']?.toDouble(),
      longitude: json['longitude']?.toDouble(),
      videoUrl: json['videoUrl'],
      virtualTourUrl: json['virtualTourUrl'],
      floorPlanUrls: (json['floorPlanUrls'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      brochureUrl: json['brochureUrl'],
      
      roofType: json['roofType'],
      numberOfFloors: json['numberOfFloors'],
      constructionPeriod: json['constructionPeriod'],
      cvBoilerBrand: json['cvBoilerBrand'],
      cvBoilerYear: json['cvBoilerYear'],
      
      brokerPhone: json['brokerPhone'],
      brokerLogoUrl: json['brokerLogoUrl'],
      
      fiberAvailable: json['fiberAvailable'],
      
      publicationDate: json['publicationDate'] != null ? DateTime.parse(json['publicationDate']) : null,
      isSoldOrRented: json['isSoldOrRented'] ?? false,
      labels: (json['labels'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
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
      
      'description': description,
      'energyLabel': energyLabel,
      'yearBuilt': yearBuilt,
      'imageUrls': imageUrls,
      
      'ownershipType': ownershipType,
      'cadastralDesignation': cadastralDesignation,
      'vveContribution': vveContribution,
      'heatingType': heatingType,
      'insulationType': insulationType,
      'gardenOrientation': gardenOrientation,
      'hasGarage': hasGarage,
      'parkingType': parkingType,
      
      'agentName': agentName,
      'volumeM3': volumeM3,
      'balconyM2': balconyM2,
      'gardenM2': gardenM2,
      'externalStorageM2': externalStorageM2,
      'features': features,
      
      'latitude': latitude,
      'longitude': longitude,
      'videoUrl': videoUrl,
      'virtualTourUrl': virtualTourUrl,
      'floorPlanUrls': floorPlanUrls,
      'brochureUrl': brochureUrl,
      
      'roofType': roofType,
      'numberOfFloors': numberOfFloors,
      'constructionPeriod': constructionPeriod,
      'cvBoilerBrand': cvBoilerBrand,
      'cvBoilerYear': cvBoilerYear,
      
      'brokerPhone': brokerPhone,
      'brokerLogoUrl': brokerLogoUrl,
      
      'fiberAvailable': fiberAvailable,
      
      'publicationDate': publicationDate?.toIso8601String(),
      'isSoldOrRented': isSoldOrRented,
      'labels': labels,
    };
  }
}
