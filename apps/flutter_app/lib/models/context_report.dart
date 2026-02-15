class ContextReport {
  ContextReport({
    required this.location,
    required this.socialMetrics,
    required this.crimeMetrics,
    required this.demographicsMetrics,
    required this.housingMetrics,
    required this.mobilityMetrics,
    required this.amenityMetrics,
    required this.environmentMetrics,
    required this.compositeScore,
    required this.categoryScores,
    required this.sources,
    required this.warnings,
    this.score,
  });

  final ContextLocation location;
  final List<ContextMetric> socialMetrics;
  final List<ContextMetric> crimeMetrics;
  final List<ContextMetric> demographicsMetrics;
  final List<ContextMetric> housingMetrics;
  final List<ContextMetric> mobilityMetrics;
  final List<ContextMetric> amenityMetrics;
  final List<ContextMetric> environmentMetrics;
  final double compositeScore;
  final Map<String, double> categoryScores;
  final List<SourceAttribution> sources;
  final List<String> warnings;
  final double? score;

  factory ContextReport.fromJson(Map<String, dynamic> json) {
    return ContextReport(
      location: ContextLocation.fromJson(json['location'] as Map<String, dynamic>),
      socialMetrics: _parseMetrics(json['socialMetrics']),
      crimeMetrics: _parseMetrics(json['crimeMetrics']),
      demographicsMetrics: _parseMetrics(json['demographicsMetrics']),
      housingMetrics: _parseMetrics(json['housingMetrics']),
      mobilityMetrics: _parseMetrics(json['mobilityMetrics']),
      amenityMetrics: _parseMetrics(json['amenityMetrics']),
      environmentMetrics: _parseMetrics(json['environmentMetrics']),
      compositeScore: (json['compositeScore'] as num?)?.toDouble() ?? 0,
      categoryScores: _parseCategoryScores(json['categoryScores']),
      sources: (json['sources'] as List<dynamic>? ?? <dynamic>[])
          .whereType<Map<String, dynamic>>()
          .map(SourceAttribution.fromJson)
          .toList(),
      warnings: (json['warnings'] as List<dynamic>? ?? <dynamic>[])
          .map((dynamic item) => item.toString())
          .toList(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'location': location.toJson(),
      'socialMetrics': socialMetrics.map((e) => e.toJson()).toList(),
      'crimeMetrics': crimeMetrics.map((e) => e.toJson()).toList(),
      'demographicsMetrics': demographicsMetrics.map((e) => e.toJson()).toList(),
      'housingMetrics': housingMetrics.map((e) => e.toJson()).toList(),
      'mobilityMetrics': mobilityMetrics.map((e) => e.toJson()).toList(),
      'amenityMetrics': amenityMetrics.map((e) => e.toJson()).toList(),
      'environmentMetrics': environmentMetrics.map((e) => e.toJson()).toList(),
      'compositeScore': compositeScore,
      'categoryScores': categoryScores,
      'sources': sources.map((e) => e.toJson()).toList(),
      'warnings': warnings,
    };
  }

  static List<ContextMetric> _parseMetrics(dynamic value) {
    final List<dynamic> list = value as List<dynamic>? ?? <dynamic>[];
    return list
        .whereType<Map<String, dynamic>>()
        .map(ContextMetric.fromJson)
        .toList();
  }

  static Map<String, double> _parseCategoryScores(dynamic value) {
    if (value == null) return {};
    final Map<String, dynamic> map = value as Map<String, dynamic>;
    return map.map((key, value) => MapEntry(key, (value as num?)?.toDouble() ?? 0));
  }
}

class ContextLocation {
  ContextLocation({
    required this.query,
    required this.displayAddress,
    required this.latitude,
    required this.longitude,
    this.rdX,
    this.rdY,
    this.municipalityCode,
    this.municipalityName,
    this.districtCode,
    this.districtName,
    this.neighborhoodCode,
    this.neighborhoodName,
    this.postalCode,
  });

  final String query;
  final String displayAddress;
  final double latitude;
  final double longitude;
  final double? rdX;
  final double? rdY;
  final String? municipalityCode;
  final String? municipalityName;
  final String? districtCode;
  final String? districtName;
  final String? neighborhoodCode;
  final String? neighborhoodName;
  final String? postalCode;

  factory ContextLocation.fromJson(Map<String, dynamic> json) {
    return ContextLocation(
      query: json['query']?.toString() ?? '',
      displayAddress: json['displayAddress']?.toString() ?? '',
      latitude: (json['latitude'] as num?)?.toDouble() ?? 0,
      longitude: (json['longitude'] as num?)?.toDouble() ?? 0,
      rdX: (json['rdX'] as num?)?.toDouble(),
      rdY: (json['rdY'] as num?)?.toDouble(),
      municipalityCode: json['municipalityCode']?.toString(),
      municipalityName: json['municipalityName']?.toString(),
      districtCode: json['districtCode']?.toString(),
      districtName: json['districtName']?.toString(),
      neighborhoodCode: json['neighborhoodCode']?.toString(),
      neighborhoodName: json['neighborhoodName']?.toString(),
      postalCode: json['postalCode']?.toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'query': query,
      'displayAddress': displayAddress,
      'latitude': latitude,
      'longitude': longitude,
      'rdX': rdX,
      'rdY': rdY,
      'municipalityCode': municipalityCode,
      'municipalityName': municipalityName,
      'districtCode': districtCode,
      'districtName': districtName,
      'neighborhoodCode': neighborhoodCode,
      'neighborhoodName': neighborhoodName,
      'postalCode': postalCode,
    };
  }
}

class ContextMetric {
  ContextMetric({
    required this.key,
    required this.label,
    required this.source,
    this.value,
    this.unit,
    this.score,
    this.note,
  });

  final String key;
  final String label;
  final double? value;
  final String? unit;
  final double? score;
  final String source;
  final String? note;

  factory ContextMetric.fromJson(Map<String, dynamic> json) {
    return ContextMetric(
      key: json['key']?.toString() ?? '',
      label: json['label']?.toString() ?? '',
      value: (json['value'] as num?)?.toDouble(),
      unit: json['unit']?.toString(),
      score: (json['score'] as num?)?.toDouble(),
      source: json['source']?.toString() ?? '',
      note: json['note']?.toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'key': key,
      'label': label,
      'source': source,
      if (value != null) 'value': value,
      if (unit != null) 'unit': unit,
      if (score != null) 'score': score,
      if (note != null) 'note': note,
    };
  }
}

class SourceAttribution {
  SourceAttribution({
    required this.source,
    required this.url,
    required this.license,
  });

  final String source;
  final String url;
  final String license;

  factory SourceAttribution.fromJson(Map<String, dynamic> json) {
    return SourceAttribution(
      source: json['source']?.toString() ?? '',
      url: json['url']?.toString() ?? '',
      license: json['license']?.toString() ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'source': source,
      'url': url,
      'license': license,
    };
  }
}

class ContextCategoryMetrics {
  ContextCategoryMetrics({
    required this.metrics,
    required this.warnings,
    this.score,
  });

  final List<ContextMetric> metrics;
  final List<String> warnings;
  final double? score;

  factory ContextCategoryMetrics.fromJson(Map<String, dynamic> json) {
    return ContextCategoryMetrics(
      metrics: (json['metrics'] as List<dynamic>? ?? [])
          .whereType<Map<String, dynamic>>()
          .map(ContextMetric.fromJson)
          .toList(),
      score: (json['score'] as num?)?.toDouble(),
      warnings: (json['warnings'] as List<dynamic>? ?? [])
          .map((e) => e.toString())
          .toList(),
    );
  }
}
