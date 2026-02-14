import '../../../models/context_report.dart';
import '../core/http_transport.dart';
import '../mappers/ai_api_mapper.dart';

class AiApiClient {
  AiApiClient({required HttpTransport transport}) : _transport = transport;

  final HttpTransport _transport;

  Future<String> getAiAnalysis(ContextReport report) {
    final Uri uri = Uri.parse('${_transport.baseUrl}/ai/analyze-report');

    return _transport.post<String>(
      uri: uri,
      timeout: const Duration(seconds: 60),
      body: <String, dynamic>{'report': report.toJson()},
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        AiApiMapper.parseSummary,
      ),
    );
  }
}
