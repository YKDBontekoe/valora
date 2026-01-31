class AppException implements Exception {
  final String message;
  final String? prefix;

  AppException(this.message, [this.prefix]);

  @override
  String toString() {
    return '${prefix ?? ''}$message';
  }
}

class NetworkException extends AppException {
  NetworkException([String message = 'Please check your internet connection'])
      : super(message, 'Network Error: ');
}

class ServerException extends AppException {
  ServerException([String? message])
      : super(message ?? 'An unexpected server error occurred', 'Server Error: ');
}

class ValidationException extends AppException {
  ValidationException([String? message])
      : super(message ?? 'Invalid request', 'Validation Error: ');
}

class NotFoundException extends AppException {
  NotFoundException([String? message])
      : super(message ?? 'Resource not found', 'Not Found: ');
}

class UnknownException extends AppException {
  UnknownException([String? message])
      : super(message ?? 'An unknown error occurred');
}
