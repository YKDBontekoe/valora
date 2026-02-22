import 'dart:convert';
import '../models/context_report.dart';
import '../services/api_client.dart';

class AiRepository {
  final ApiClient _client;

  AiRepository(this._client);

  Future<String> getAiAnalysis(ContextReport report) async {
    final uri = Uri.parse('${ApiClient.baseUrl}/ai/analyze-report');
    try {
      final payload = json.encode({
        'report': report.toJson(),
      });

      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) => _client.client
              .post(uri, headers: headers, body: payload)
              .timeout(const Duration(seconds: 60)), // AI takes longer
        ),
      );

      return _client.handleResponse(
        response,
        (body) {
          final jsonBody = json.decode(body);
          return jsonBody['summary'] as String;
        },
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, uri);
    }
  }
}
