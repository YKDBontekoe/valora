import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/core/utils/error_message_utils.dart';

void main() {
  group('ErrorMessageUtils', () {
    test('getUserFriendlyMessage returns specific messages for known exceptions', () {
      expect(ErrorMessageUtils.getUserFriendlyMessage(NetworkException('Net Error')), 'Net Error');
      expect(ErrorMessageUtils.getUserFriendlyMessage(ServerException('Server Error')), 'Server Error');
      expect(ErrorMessageUtils.getUserFriendlyMessage(NotFoundException('Not Found')), 'Not Found');
      expect(ErrorMessageUtils.getUserFriendlyMessage(ValidationException('Invalid')), 'Invalid');
      expect(ErrorMessageUtils.getUserFriendlyMessage(AppException('Generic')), 'Generic');
    });

    test('getUserFriendlyMessage returns default message for unknown exceptions', () {
      expect(ErrorMessageUtils.getUserFriendlyMessage(Exception('Unknown')), 'An unexpected error occurred.');
      expect(ErrorMessageUtils.getUserFriendlyMessage(Error()), 'An unexpected error occurred.');
      expect(ErrorMessageUtils.getUserFriendlyMessage('String Error'), 'An unexpected error occurred.');
    });
  });
}
