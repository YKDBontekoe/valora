class SupportStatus {
  final bool isSupportActive;
  final String supportMessage;
  final String? statusPageUrl;
  final String? contactEmail;

  SupportStatus({
    required this.isSupportActive,
    required this.supportMessage,
    this.statusPageUrl,
    this.contactEmail,
  });

  factory SupportStatus.fromJson(Map<String, dynamic> json) {
    return SupportStatus(
      isSupportActive: json['isSupportActive'] as bool? ?? true,
      supportMessage: json['supportMessage'] as String? ?? 'Our support team is available.',
      statusPageUrl: json['statusPageUrl'] as String?,
      contactEmail: json['contactEmail'] as String?,
    );
  }

  /// Default fallback state when API fails
  factory SupportStatus.fallback() {
    return SupportStatus(
      isSupportActive: true,
      supportMessage: 'Support is available.',
      contactEmail: 'support@valora.nl',
    );
  }
}
