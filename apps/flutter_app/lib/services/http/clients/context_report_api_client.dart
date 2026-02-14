import '../../../models/context_report.dart';
import '../core/api_runner.dart';
import '../core/http_transport.dart';
import '../mappers/context_report_api_mapper.dart';

class ContextReportApiClient {
  ContextReportApiClient({
    required HttpTransport transport,
    required ApiRunner runner,
  })  : _transport = transport,
        _runner = runner;

  final HttpTransport _transport;
  final ApiRunner _runner;

  Future<ContextReport> getContextReport(
    String input, {
    int radiusMeters = 1000,
  }) {
    final Uri uri = Uri.parse('${_transport.baseUrl}/context/report');

    return _transport.post<ContextReport>(
      uri: uri,
      body: <String, dynamic>{
        'input': input,
        'radiusMeters': radiusMeters,
      },
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        (body) => _runner(ContextReportApiMapper.parseContextReport, body),
      ),
    );
  }
}
