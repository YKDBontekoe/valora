import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/report/context_report_skeleton.dart';
import 'package:valora_app/widgets/common/valora_shimmer.dart';

void main() {
  testWidgets('ContextReportSkeleton renders shimmers', (tester) async {
    // Avoid infinite timer error by not letting animations run forever
    await tester.runAsync(() async {
      await tester.pumpWidget(
        const MaterialApp(home: Scaffold(body: ContextReportSkeleton())),
      );
      await tester.pump(const Duration(milliseconds: 100));

      expect(find.byType(ValoraShimmer), findsAtLeastNWidgets(5));
    });
  });
}
