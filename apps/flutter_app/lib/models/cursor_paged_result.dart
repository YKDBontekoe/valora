class CursorPagedResult<T> {
  final List<T> items;
  final String? nextCursor;
  final bool hasMore;

  CursorPagedResult({
    required this.items,
    this.nextCursor,
    required this.hasMore,
  });

  factory CursorPagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(dynamic) fromJsonT,
  ) {
    return CursorPagedResult<T>(
      items: (json['items'] as List).map(fromJsonT).toList(),
      nextCursor: json['nextCursor'] as String?,
      hasMore: json['hasMore'] as bool,
    );
  }
}
