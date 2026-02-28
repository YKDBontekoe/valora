class ListingSearchRequest {
  final String? city;
  final String? postalCode;
  final double? minLat;
  final double? minLon;
  final double? maxLat;
  final double? maxLon;
  final double? minPrice;
  final double? maxPrice;
  final int? minArea;
  final String? propertyType;
  final String? energyLabel;
  final int? minYearBuilt;
  final int? maxYearBuilt;
  final String sortBy;
  final int page;
  final int pageSize;

  const ListingSearchRequest({
    this.city,
    this.postalCode,
    this.minLat,
    this.minLon,
    this.maxLat,
    this.maxLon,
    this.minPrice,
    this.maxPrice,
    this.minArea,
    this.propertyType,
    this.energyLabel,
    this.minYearBuilt,
    this.maxYearBuilt,
    this.sortBy = 'newest',
    this.page = 1,
    this.pageSize = 20,
  });

  Map<String, String> toQueryParameters() {
    final params = <String, String>{
      'sortBy': sortBy,
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };

    if (city != null) params['city'] = city!;
    if (postalCode != null) params['postalCode'] = postalCode!;
    if (minLat != null) params['minLat'] = minLat.toString();
    if (minLon != null) params['minLon'] = minLon.toString();
    if (maxLat != null) params['maxLat'] = maxLat.toString();
    if (maxLon != null) params['maxLon'] = maxLon.toString();
    if (minPrice != null) params['minPrice'] = minPrice.toString();
    if (maxPrice != null) params['maxPrice'] = maxPrice.toString();
    if (minArea != null) params['minArea'] = minArea.toString();
    if (propertyType != null) params['propertyType'] = propertyType!;
    if (energyLabel != null) params['energyLabel'] = energyLabel!;
    if (minYearBuilt != null) params['minYearBuilt'] = minYearBuilt.toString();
    if (maxYearBuilt != null) params['maxYearBuilt'] = maxYearBuilt.toString();

    return params;
  }
}
