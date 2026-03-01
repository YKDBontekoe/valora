class SavedProperty {
  final String id;
  final String propertyId;
  final PropertySummary? property;
  final String addedByUserId;
  final String? notes;
  final DateTime addedAt;
  final int commentCount;

  SavedProperty({
    required this.id,
    required this.propertyId,
    this.property,
    required this.addedByUserId,
    this.notes,
    required this.addedAt,
    required this.commentCount,
  });

  factory SavedProperty.fromJson(Map<String, dynamic> json) {
    if (json['id'] == null || json['propertyId'] == null || json['addedByUserId'] == null) {
      throw const FormatException('Missing required fields in SavedProperty JSON');
    }
    return SavedProperty(
      id: json['id'] as String,
      propertyId: json['propertyId'] as String,
      property: json['property'] != null ? PropertySummary.fromJson(json['property']) : null,
      addedByUserId: json['addedByUserId'] as String,
      notes: json['notes'] as String?,
      addedAt: json['addedAt'] != null ? DateTime.parse(json['addedAt'] as String) : DateTime.now(),
      commentCount: (json['commentCount'] as num?)?.toInt() ?? 0,
    );
  }
}

class PropertySummary {
  final String id;
  final String address;
  final String? city;
  final int? livingAreaM2;
  final double? safetyScore;
  final double? compositeScore;

  PropertySummary({
    required this.id,
    required this.address,
    this.city,
    this.livingAreaM2,
    this.safetyScore,
    this.compositeScore,
  });

  factory PropertySummary.fromJson(Map<String, dynamic> json) {
    if (json['id'] == null || json['address'] == null) {
      throw const FormatException('Missing required fields in PropertySummary JSON');
    }
    return PropertySummary(
      id: json['id'] as String,
      address: json['address'] as String,
      city: json['city'] as String?,
      livingAreaM2: (json['livingAreaM2'] as num?)?.toInt(),
      safetyScore: json['safetyScore'] != null ? (json['safetyScore'] as num).toDouble() : null,
      compositeScore: json['compositeScore'] != null ? (json['compositeScore'] as num).toDouble() : null,
    );
  }
}
