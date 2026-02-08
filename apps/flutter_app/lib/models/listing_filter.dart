class ListingFilter {
  final int page;
  final int pageSize;
  final String? searchTerm;
  final double? minPrice;
  final double? maxPrice;
  final String? city;
  final int? minBedrooms;
  final int? minLivingArea;
  final int? maxLivingArea;
  final double? minSafetyScore;
  final double? minCompositeScore;
  final String? sortBy;
  final String? sortOrder;

  const ListingFilter({
    this.page = 1,
    this.pageSize = 10,
    this.searchTerm,
    this.minPrice,
    this.maxPrice,
    this.city,
    this.minBedrooms,
    this.minLivingArea,
    this.maxLivingArea,
    this.minSafetyScore,
    this.minCompositeScore,
    this.sortBy,
    this.sortOrder,
  });

  ListingFilter copyWith({
    int? page,
    int? pageSize,
    String? searchTerm,
    double? minPrice,
    double? maxPrice,
    String? city,
    int? minBedrooms,
    int? minLivingArea,
    int? maxLivingArea,
    double? minSafetyScore,
    double? minCompositeScore,
    String? sortBy,
    String? sortOrder,
  }) {
    return ListingFilter(
      page: page ?? this.page,
      pageSize: pageSize ?? this.pageSize,
      searchTerm: searchTerm ?? this.searchTerm,
      minPrice: minPrice ?? this.minPrice,
      maxPrice: maxPrice ?? this.maxPrice,
      city: city ?? this.city,
      minBedrooms: minBedrooms ?? this.minBedrooms,
      minLivingArea: minLivingArea ?? this.minLivingArea,
      maxLivingArea: maxLivingArea ?? this.maxLivingArea,
      minSafetyScore: minSafetyScore ?? this.minSafetyScore,
      minCompositeScore: minCompositeScore ?? this.minCompositeScore,
      sortBy: sortBy ?? this.sortBy,
      sortOrder: sortOrder ?? this.sortOrder,
    );
  }

  Map<String, String> toQueryParameters() {
    final params = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };

    if (searchTerm != null && searchTerm!.isNotEmpty) {
      params['searchTerm'] = searchTerm!;
    }
    if (minPrice != null) {
      params['minPrice'] = minPrice.toString();
    }
    if (maxPrice != null) {
      params['maxPrice'] = maxPrice.toString();
    }
    if (city != null && city!.isNotEmpty) {
      params['city'] = city!;
    }
    if (minBedrooms != null) {
      params['minBedrooms'] = minBedrooms.toString();
    }
    if (minLivingArea != null) {
      params['minLivingArea'] = minLivingArea.toString();
    }
    if (maxLivingArea != null) {
      params['maxLivingArea'] = maxLivingArea.toString();
    }
    if (minSafetyScore != null) {
      params['minSafetyScore'] = minSafetyScore.toString();
    }
    if (minCompositeScore != null) {
      params['minCompositeScore'] = minCompositeScore.toString();
    }
    if (sortBy != null && sortBy!.isNotEmpty) {
      params['sortBy'] = sortBy!;
    }
    if (sortOrder != null && sortOrder!.isNotEmpty) {
      params['sortOrder'] = sortOrder!;
    }

    return params;
  }
}
