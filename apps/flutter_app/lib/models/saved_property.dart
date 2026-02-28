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
    return SavedProperty(
      id: json['id'],
      propertyId: json['propertyId'],
      property: json['property'] != null ? PropertySummary.fromJson(json['property']) : null,
      addedByUserId: json['addedByUserId'],
      notes: json['notes'],
      addedAt: DateTime.parse(json['addedAt']),
      commentCount: json['commentCount'],
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
    return PropertySummary(
      id: json['id'],
      address: json['address'],
      city: json['city'],
      livingAreaM2: json['livingAreaM2'],
      safetyScore: json['safetyScore'] != null ? (json['safetyScore'] as num).toDouble() : null,
      compositeScore: json['compositeScore'] != null ? (json['compositeScore'] as num).toDouble() : null,
    );
  }
}
