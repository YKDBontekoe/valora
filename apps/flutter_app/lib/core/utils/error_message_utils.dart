import '../exceptions/app_exceptions.dart';

class ErrorMessageUtils {
  static String getUserFriendlyMessage(Object error) {
    if (error is NetworkException) {
      return error.message;
    } else if (error is ServerException) {
      return error.message;
    } else if (error is NotFoundException) {
      return error.message;
    } else if (error is ValidationException) {
      return error.message;
    } else if (error is AppException) {
      return error.message;
    }
    return 'An unexpected error occurred.';
  }
}
