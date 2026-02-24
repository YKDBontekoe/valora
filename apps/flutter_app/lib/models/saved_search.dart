class SavedSearch {
  final String id;
  final String query;
  final int radiusMeters;
  final DateTime createdAt;
  final bool isAlertEnabled;

  SavedSearch({
    required this.id,
    required this.query,
    required this.radiusMeters,
    required this.createdAt,
    this.isAlertEnabled = false,
  });

  SavedSearch copyWith({
    String? id,
    String? query,
    int? radiusMeters,
    DateTime? createdAt,
    bool? isAlertEnabled,
  }) {
    return SavedSearch(
      id: id ?? this.id,
      query: query ?? this.query,
      radiusMeters: radiusMeters ?? this.radiusMeters,
      createdAt: createdAt ?? this.createdAt,
      isAlertEnabled: isAlertEnabled ?? this.isAlertEnabled,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'query': query,
      'radiusMeters': radiusMeters,
      'createdAt': createdAt.toIso8601String(),
      'isAlertEnabled': isAlertEnabled,
    };
  }

  factory SavedSearch.fromJson(Map<String, dynamic> json) {
    return SavedSearch(
      id: json['id'] as String,
      query: json['query'] as String,
      radiusMeters: json['radiusMeters'] as int,
      createdAt: DateTime.parse(json['createdAt'] as String),
      isAlertEnabled: json['isAlertEnabled'] as bool? ?? false,
    );
  }
}
