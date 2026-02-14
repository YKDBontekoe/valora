import 'dart:convert';

class AiApiMapper {
  const AiApiMapper._();

  static String parseSummary(String body) {
    final Map<String, dynamic> jsonBody = json.decode(body) as Map<String, dynamic>;
    return jsonBody['summary'] as String;
  }
}
