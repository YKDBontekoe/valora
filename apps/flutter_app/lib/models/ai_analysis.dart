class AiAnalysis {
  final String summary;
  final List<String> topPositives;
  final List<String> topConcerns;
  final int confidence;
  final String disclaimer;

  const AiAnalysis({
    required this.summary,
    required this.topPositives,
    required this.topConcerns,
    required this.confidence,
    required this.disclaimer,
  });

  factory AiAnalysis.fromJson(Map<String, dynamic> json) {
    return AiAnalysis(
      summary: json['summary'] as String? ?? '',
      topPositives: (json['topPositives'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      topConcerns: (json['topConcerns'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      confidence: json['confidence'] as int? ?? 0,
      disclaimer: json['disclaimer'] as String? ?? '',
    );
  }
}
