import 'dart:convert';
import '../models/context_report.dart';
import '../services/api_client.dart';

class ContextReportRepository {
  final ApiClient _client;

  ContextReportRepository(this._client);

  Future<ContextReport> getContextReport(
    String input, {
    int radiusMeters = 1000,
  }) async {
    final uri = Uri.parse('${ApiClient.baseUrl}/context/report');
    try {
      final payload = json.encode(<String, dynamic>{
        'input': input,
        'radiusMeters': radiusMeters,
      });

      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) => _client.client
              .post(uri, headers: headers, body: payload)
              .timeout(ApiClient.timeoutDuration),
        ),
      );

      return await _client.handleResponse(
        response,
        (body) => _client.runner(_parseContextReport, body),
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, uri);
    }
  }

  static ContextReport _parseContextReport(String body) {
    return ContextReport.fromJson(json.decode(body));
  }
}
