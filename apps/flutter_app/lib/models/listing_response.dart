import 'listing.dart';

class ListingResponse {
  final List<Listing> items;
  final int pageIndex;
  final int totalPages;
  final int totalCount;
  final bool hasNextPage;
  final bool hasPreviousPage;

  ListingResponse({
    required this.items,
    required this.pageIndex,
    required this.totalPages,
    required this.totalCount,
    required this.hasNextPage,
    required this.hasPreviousPage,
  });

  factory ListingResponse.fromJson(Map<String, dynamic> json) {
    return ListingResponse(
      items: (json['items'] as List).map((i) => Listing.fromJson(i)).toList(),
      pageIndex: json['pageIndex'],
      totalPages: json['totalPages'],
      totalCount: json['totalCount'],
      hasNextPage: json['hasNextPage'],
      hasPreviousPage: json['hasPreviousPage'],
    );
  }
}
