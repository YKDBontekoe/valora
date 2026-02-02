class ListingFilter {
  final int page;
  final int pageSize;
  final String? searchTerm;
  final double? minPrice;
  final double? maxPrice;
  final String? city;
  final List<String>? cities;
  final int? minBedrooms;
  final int? minLivingArea;
  final int? maxLivingArea;
  final String? sortBy;
  final String? sortOrder;

  const ListingFilter({
    this.page = 1,
    this.pageSize = 10,
    this.searchTerm,
    this.minPrice,
    this.maxPrice,
    this.city,
    this.cities,
    this.minBedrooms,
    this.minLivingArea,
    this.maxLivingArea,
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
    List<String>? cities,
    int? minBedrooms,
    int? minLivingArea,
    int? maxLivingArea,
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
      cities: cities ?? this.cities,
      minBedrooms: minBedrooms ?? this.minBedrooms,
      minLivingArea: minLivingArea ?? this.minLivingArea,
      maxLivingArea: maxLivingArea ?? this.maxLivingArea,
      sortBy: sortBy ?? this.sortBy,
      sortOrder: sortOrder ?? this.sortOrder,
    );
  }

  Map<String, dynamic> toQueryParameters() {
    final params = <String, dynamic>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };

    if (searchTerm != null && searchTerm!.isNotEmpty) {
      params['searchTerm'] = searchTerm!;
    }
    if (minPrice != null) params['minPrice'] = minPrice.toString();
    if (maxPrice != null) params['maxPrice'] = maxPrice.toString();
    if (city != null && city!.isNotEmpty) params['city'] = city!;
    if (cities != null && cities!.isNotEmpty) params['cities'] = cities!;
    if (minBedrooms != null) params['minBedrooms'] = minBedrooms.toString();
    if (minLivingArea != null) params['minLivingArea'] = minLivingArea.toString();
    if (maxLivingArea != null) params['maxLivingArea'] = maxLivingArea.toString();
    if (sortBy != null && sortBy!.isNotEmpty) params['sortBy'] = sortBy!;
    if (sortOrder != null && sortOrder!.isNotEmpty) params['sortOrder'] = sortOrder!;

    return params;
  }
}
