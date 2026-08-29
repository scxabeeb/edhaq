import 'package:dio/dio.dart';
import '../data/errors/failures.dart';

/// Converts a [DioException] into the appropriate [Failure] subclass.
///
/// This centralises the error-mapping logic so repositories can simply
/// wrap API calls in a try/catch and delegate to this function.
Failure mapDioExceptionToFailure(DioException exception) {
  switch (exception.type) {
    case DioExceptionType.connectionTimeout:
    case DioExceptionType.sendTimeout:
    case DioExceptionType.receiveTimeout:
      return const NetworkFailure(
          'Request timed out. Please check your internet connection and try again.');
    case DioExceptionType.connectionError:
      return const NetworkFailure(
          'No internet connection. Please connect to the internet and try again.');
    case DioExceptionType.badResponse:
      final statusCode = exception.response?.statusCode;
      final message = _extractMessage(exception) ??
          _defaultMessageForStatus(statusCode);
      if (statusCode == 401 || statusCode == 403) {
        return AuthenticationFailure(message);
      }
      if (statusCode == 400 || statusCode == 422) {
        return ValidationFailure(message);
      }
      return ServerFailure(message, code: statusCode);
    case DioExceptionType.cancel:
      return const ServerFailure('Request was cancelled.');
    case DioExceptionType.unknown:
    default:
      return const ServerFailure(
          'Something went wrong. Please try again later.');
  }
}

/// Attempts to extract a human-readable message from the response body.
String? _extractMessage(DioException exception) {
  final data = exception.response?.data;
  if (data is Map<String, dynamic>) {
    // ProblemDetails (RFC 7807): { title, detail, ... }
    if (data['title'] is String && (data['title'] as String).isNotEmpty) {
      return data['title'] as String;
    }
    if (data['detail'] is String && (data['detail'] as String).isNotEmpty) {
      return data['detail'] as String;
    }
    if (data['message'] is String) {
      return data['message'] as String;
    }
    if (data['error'] is String) {
      return data['error'] as String;
    }
  }
  if (data is String && data.isNotEmpty) {
    return data;
  }
  return null;
}

String _defaultMessageForStatus(int? statusCode) {
  if (statusCode == null) {
    return 'An unexpected error occurred.';
  }
  return switch (statusCode) {
    400 => 'Bad request. Please check your input.',
    401 => 'Authentication is required. Please log in.',
    403 => 'You do not have permission to perform this action.',
    404 => 'The requested resource was not found.',
    500 => 'Server error. Please try again later.',
    _ => 'An error occurred (HTTP $statusCode).',
  };
}
