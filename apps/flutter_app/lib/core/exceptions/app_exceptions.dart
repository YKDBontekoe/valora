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

class UnauthorizedException extends AppException {
  UnauthorizedException([String? message])
    : super(message ?? 'Unauthorized access', 'Unauthorized: ');
}

class ForbiddenException extends AppException {
  ForbiddenException([String? message])
    : super(message ?? 'Forbidden access', 'Forbidden: ');
}

class UnknownException extends AppException {
  UnknownException([String? message])
    : super(message ?? 'An unknown error occurred');
}

class JsonParsingException extends AppException {
  JsonParsingException([String? message])
    : super(message ?? 'Failed to parse server response', 'Data Error: ');
}

class RefreshTokenInvalidException extends AppException {
  RefreshTokenInvalidException([String? message])
    : super(message ?? 'Refresh token is invalid', 'Authentication Error: ');
}

class StorageException extends AppException {
  StorageException([String? message])
    : super(message ?? 'Secure storage access failed', 'Storage Error: ');
}

class TransientHttpException extends AppException {
  TransientHttpException([String? message])
    : super(message ?? 'Service is temporarily unavailable', 'System Error: ');
}
