import 'dart:convert';

import '../../../models/context_report.dart';

class ContextReportApiMapper {
  const ContextReportApiMapper._();

  static ContextReport parseContextReport(String body) {
    return ContextReport.fromJson(json.decode(body) as Map<String, dynamic>);
  }
}
