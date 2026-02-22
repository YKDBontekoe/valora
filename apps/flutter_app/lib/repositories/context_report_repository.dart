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
    final response = await _client.post(
      '/context/report',
      data: {
        'input': input,
        'radiusMeters': radiusMeters,
      },
    );

    return _client.handleResponse(
      response,
      (body) => ContextReport.fromJson(json.decode(body)),
    );
  }

  Future<String> getAiAnalysis(ContextReport report) async {
    final response = await _client.post(
      '/ai/analyze-report',
      data: {
        'report': report.toJson(),
      },
      timeout: const Duration(seconds: 60),
    );

    return _client.handleResponse(
      response,
      (body) {
        final jsonBody = json.decode(body);
        return jsonBody['summary'] as String;
      },
    );
  }
}
