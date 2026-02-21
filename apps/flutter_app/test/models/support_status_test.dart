import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/support_status.dart';

void main() {
  group('SupportStatus', () {
    test('fromJson parses full JSON correctly', () {
      final json = {
        'isSupportActive': false,
        'supportMessage': 'Maintenance',
        'statusPageUrl': 'https://status.example.com',
        'contactEmail': 'help@example.com',
      };

      final status = SupportStatus.fromJson(json);

      expect(status.isSupportActive, false);
      expect(status.supportMessage, 'Maintenance');
      expect(status.statusPageUrl, 'https://status.example.com');
      expect(status.contactEmail, 'help@example.com');
    });

    test('fromJson handles partial JSON with defaults', () {
      final json = <String, dynamic>{};

      final status = SupportStatus.fromJson(json);

      expect(status.isSupportActive, true);
      expect(status.supportMessage, 'Our support team is available.');
      expect(status.statusPageUrl, null);
      expect(status.contactEmail, null);
    });

    test('fallback returns expected default values', () {
      final status = SupportStatus.fallback();

      expect(status.isSupportActive, true);
      expect(status.supportMessage, 'Support is available.');
      expect(status.statusPageUrl, null);
      expect(status.contactEmail, 'support@valora.nl');
    });
  });
}
