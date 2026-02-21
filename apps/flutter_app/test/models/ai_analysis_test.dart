import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/ai_analysis.dart';

void main() {
  group('AiAnalysis', () {
    test('fromJson parses full data correctly', () {
      final json = {
        'summary': 'Summary text',
        'topPositives': ['Pro 1', 'Pro 2'],
        'topConcerns': ['Con 1'],
        'confidence': 80,
        'disclaimer': 'Disclaimer text',
      };

      final analysis = AiAnalysis.fromJson(json);

      expect(analysis.summary, 'Summary text');
      expect(analysis.topPositives, ['Pro 1', 'Pro 2']);
      expect(analysis.topConcerns, ['Con 1']);
      expect(analysis.confidence, 80);
      expect(analysis.disclaimer, 'Disclaimer text');
    });

    test('fromJson handles null values with defaults', () {
      final json = <String, dynamic>{};

      final analysis = AiAnalysis.fromJson(json);

      expect(analysis.summary, '');
      expect(analysis.topPositives, isEmpty);
      expect(analysis.topConcerns, isEmpty);
      expect(analysis.confidence, 0);
      expect(analysis.disclaimer, '');
    });

    test('fromJson handles partially populated data', () {
      final json = {
        'summary': 'Partial summary',
        'confidence': 50,
      };

      final analysis = AiAnalysis.fromJson(json);

      expect(analysis.summary, 'Partial summary');
      expect(analysis.topPositives, isEmpty);
      expect(analysis.topConcerns, isEmpty);
      expect(analysis.confidence, 50);
      expect(analysis.disclaimer, '');
    });
  });
}
